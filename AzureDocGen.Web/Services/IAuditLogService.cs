using AzureDocGen.Data.Entities;

namespace AzureDocGen.Web.Services;

/// <summary>
/// 監査ログサービスのインターフェース
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// 監査ログを記録
    /// </summary>
    Task LogAsync(string action, string entityType, Guid? entityId, string userId,
        string? details = null, string? ipAddress = null);

    /// <summary>
    /// 監査ログを取得
    /// </summary>
    Task<AuditLog?> GetAuditLogByIdAsync(Guid id);

    /// <summary>
    /// 監査ログ一覧を取得（ページネーション付き）
    /// </summary>
    Task<(List<AuditLog> logs, int totalCount)> GetAuditLogsAsync(
        string? userId = null,
        string? action = null,
        string? entityType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 50);

    /// <summary>
    /// エンティティの監査ログを取得
    /// </summary>
    Task<List<AuditLog>> GetEntityAuditLogsAsync(string entityType, Guid entityId);

    /// <summary>
    /// ユーザーの監査ログを取得
    /// </summary>
    Task<List<AuditLog>> GetUserAuditLogsAsync(string userId, int limit = 100);

    /// <summary>
    /// 監査ログの統計を取得
    /// </summary>
    Task<AuditLogStatistics> GetAuditLogStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// 古い監査ログを削除（保持期間を超えたもの）
    /// </summary>
    Task<int> CleanupOldLogsAsync(int retentionDays = 365);
}

/// <summary>
/// 監査ログ統計
/// </summary>
public class AuditLogStatistics
{
    public int TotalLogs { get; set; }
    public Dictionary<string, int> ActionCounts { get; set; } = new();
    public Dictionary<string, int> EntityTypeCounts { get; set; } = new();
    public Dictionary<string, int> UserCounts { get; set; } = new();
    public DateTime? OldestLog { get; set; }
    public DateTime? NewestLog { get; set; }
}
