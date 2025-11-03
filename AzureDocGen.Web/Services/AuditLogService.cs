using Microsoft.EntityFrameworkCore;
using AzureDocGen.Data.Contexts;
using AzureDocGen.Data.Entities;

namespace AzureDocGen.Web.Services;

/// <summary>
/// 監査ログサービスの実装
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(
        ApplicationDbContext context,
        ILogger<AuditLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(string action, string entityType, Guid? entityId,
        string userId, string? details = null, string? ipAddress = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                Details = details,
                IpAddress = ipAddress
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Audit log created: {Action} on {EntityType} by {UserId}",
                action, entityType, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log");
            // 監査ログの失敗は業務処理を止めないようにする
        }
    }

    public async Task<AuditLog?> GetAuditLogByIdAsync(Guid id)
    {
        return await _context.AuditLogs
            .Include(al => al.User)
            .FirstOrDefaultAsync(al => al.Id == id);
    }

    public async Task<(List<AuditLog> logs, int totalCount)> GetAuditLogsAsync(
        string? userId = null,
        string? action = null,
        string? entityType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _context.AuditLogs
            .Include(al => al.User)
            .AsQueryable();

        // フィルター適用
        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(al => al.UserId == userId);
        }

        if (!string.IsNullOrEmpty(action))
        {
            query = query.Where(al => al.Action == action);
        }

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(al => al.EntityType == entityType);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(al => al.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(al => al.Timestamp <= toDate.Value);
        }

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(al => al.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (logs, totalCount);
    }

    public async Task<List<AuditLog>> GetEntityAuditLogsAsync(string entityType, Guid entityId)
    {
        return await _context.AuditLogs
            .Include(al => al.User)
            .Where(al => al.EntityType == entityType && al.EntityId == entityId)
            .OrderByDescending(al => al.Timestamp)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetUserAuditLogsAsync(string userId, int limit = 100)
    {
        return await _context.AuditLogs
            .Where(al => al.UserId == userId)
            .OrderByDescending(al => al.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<AuditLogStatistics> GetAuditLogStatisticsAsync(
        DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(al => al.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(al => al.Timestamp <= toDate.Value);
        }

        var logs = await query.ToListAsync();

        var statistics = new AuditLogStatistics
        {
            TotalLogs = logs.Count,
            ActionCounts = logs.GroupBy(al => al.Action)
                              .ToDictionary(g => g.Key, g => g.Count()),
            EntityTypeCounts = logs.GroupBy(al => al.EntityType)
                                  .ToDictionary(g => g.Key, g => g.Count()),
            UserCounts = logs.GroupBy(al => al.UserId)
                            .OrderByDescending(g => g.Count())
                            .Take(10)
                            .ToDictionary(g => g.Key, g => g.Count()),
            OldestLog = logs.Any() ? logs.Min(al => al.Timestamp) : null,
            NewestLog = logs.Any() ? logs.Max(al => al.Timestamp) : null
        };

        return statistics;
    }

    public async Task<int> CleanupOldLogsAsync(int retentionDays = 365)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

        var oldLogs = await _context.AuditLogs
            .Where(al => al.Timestamp < cutoffDate)
            .ToListAsync();

        if (oldLogs.Any())
        {
            _context.AuditLogs.RemoveRange(oldLogs);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} old audit logs", oldLogs.Count);
        }

        return oldLogs.Count;
    }
}
