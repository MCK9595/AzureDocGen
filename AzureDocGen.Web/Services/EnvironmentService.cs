using Microsoft.EntityFrameworkCore;
using AzureDocGen.Data.Contexts;
using AzureDocGen.Data.Entities;
using AzureDocGen.Data.Enums;

namespace AzureDocGen.Web.Services;

/// <summary>
/// 環境管理サービスの実装
/// </summary>
public class EnvironmentService : IEnvironmentService
{
    private readonly ApplicationDbContext _context;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<EnvironmentService> _logger;

    public EnvironmentService(
        ApplicationDbContext context,
        IPermissionService permissionService,
        ILogger<EnvironmentService> logger)
    {
        _context = context;
        _permissionService = permissionService;
        _logger = logger;
    }

    public async Task<AzureDocGen.Data.Entities.Environment> CreateEnvironmentAsync(Guid projectId, string name, string description, string createdBy)
    {
        // ユーザーの存在確認
        var user = await _context.Users.FindAsync(createdBy);
        if (user == null)
        {
            throw new InvalidOperationException($"User {createdBy} not found");
        }

        // プロジェクトの存在確認
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null)
        {
            throw new InvalidOperationException($"Project {projectId} not found");
        }

        // 同じプロジェクト内での環境名重複チェック
        var existingEnvironment = await _context.Environments
            .Where(e => e.ProjectId == projectId && e.Name == name)
            .FirstOrDefaultAsync();

        if (existingEnvironment != null)
        {
            throw new InvalidOperationException($"Environment with name '{name}' already exists in this project");
        }

        // 表示順序を設定（既存の環境数 + 1）
        var currentCount = await _context.Environments
            .Where(e => e.ProjectId == projectId)
            .CountAsync();

        var environment = new AzureDocGen.Data.Entities.Environment
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = name,
            Description = description,
            DisplayOrder = currentCount + 1,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        _context.Environments.Add(environment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Environment {EnvironmentId} created for project {ProjectId} by {UserId}", 
            environment.Id, projectId, createdBy);

        return environment;
    }

    public async Task<AzureDocGen.Data.Entities.Environment?> GetEnvironmentByIdAsync(Guid environmentId)
    {
        return await _context.Environments
            .Include(e => e.Project)
            .Include(e => e.Creator)
            .FirstOrDefaultAsync(e => e.Id == environmentId);
    }

    public async Task<List<AzureDocGen.Data.Entities.Environment>> GetProjectEnvironmentsAsync(Guid projectId)
    {
        return await _context.Environments
            .Include(e => e.Creator)
            .Where(e => e.ProjectId == projectId)
            .OrderBy(e => e.DisplayOrder)
            .ThenBy(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<AzureDocGen.Data.Entities.Environment> UpdateEnvironmentAsync(Guid environmentId, string name, string description)
    {
        var environment = await _context.Environments.FindAsync(environmentId);
        if (environment == null)
        {
            throw new InvalidOperationException($"Environment {environmentId} not found");
        }

        // 同じプロジェクト内での環境名重複チェック（自分以外）
        var existingEnvironment = await _context.Environments
            .Where(e => e.ProjectId == environment.ProjectId && 
                       e.Name == name && 
                       e.Id != environmentId)
            .FirstOrDefaultAsync();

        if (existingEnvironment != null)
        {
            throw new InvalidOperationException($"Environment with name '{name}' already exists in this project");
        }

        environment.Name = name;
        environment.Description = description;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Environment {EnvironmentId} updated", environmentId);

        return environment;
    }

    public async Task<bool> DeleteEnvironmentAsync(Guid environmentId, string deletedBy)
    {
        var environment = await _context.Environments
            .Include(e => e.DesignDocuments)
            .FirstOrDefaultAsync(e => e.Id == environmentId);

        if (environment == null)
        {
            return false;
        }

        // 設計書が存在する場合は削除不可
        if (environment.DesignDocuments?.Any() == true)
        {
            throw new InvalidOperationException("Cannot delete environment with existing design documents");
        }

        var projectId = environment.ProjectId;
        var deletedOrder = environment.DisplayOrder;

        _context.Environments.Remove(environment);

        // 削除した環境より後の順序を調整
        var subsequentEnvironments = await _context.Environments
            .Where(e => e.ProjectId == projectId && e.DisplayOrder > deletedOrder)
            .ToListAsync();

        foreach (var env in subsequentEnvironments)
        {
            env.DisplayOrder--;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Environment {EnvironmentId} deleted by {UserId}", environmentId, deletedBy);

        return true;
    }

    public async Task<bool> UpdateEnvironmentOrderAsync(Guid projectId, List<Guid> orderedEnvironmentIds)
    {
        var environments = await _context.Environments
            .Where(e => e.ProjectId == projectId)
            .ToListAsync();

        for (int i = 0; i < orderedEnvironmentIds.Count; i++)
        {
            var environment = environments.FirstOrDefault(e => e.Id == orderedEnvironmentIds[i]);
            if (environment != null)
            {
                environment.DisplayOrder = i + 1;
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Environment order updated for project {ProjectId}", projectId);

        return true;
    }

    public Task<bool> HasDesignDocumentsAsync(Guid environmentId)
    {
        // Note: Currently DesignDocument doesn't have EnvironmentId, so returning false for now
        // TODO: Implement when DesignDocument-Environment relationship is established
        return Task.FromResult(false);
    }

    public async Task<bool> CanUserAccessEnvironmentAsync(string userId, Guid environmentId)
    {
        var environment = await _context.Environments
            .Include(e => e.Project)
            .FirstOrDefaultAsync(e => e.Id == environmentId);

        if (environment == null)
        {
            return false;
        }

        // システム管理者は常にアクセス可能
        if (await _permissionService.HasSystemRoleAsync(userId, SystemRoleType.SystemAdministrator))
        {
            return true;
        }

        // プロジェクトメンバーかチェック
        return await _permissionService.HasProjectRoleAsync(userId, environment.ProjectId, ProjectRoleType.ProjectViewer);
    }

    public async Task<bool> CanUserEditEnvironmentAsync(string userId, Guid environmentId)
    {
        var environment = await _context.Environments
            .Include(e => e.Project)
            .FirstOrDefaultAsync(e => e.Id == environmentId);

        if (environment == null)
        {
            return false;
        }

        // システム管理者は常に編集可能
        if (await _permissionService.HasSystemRoleAsync(userId, SystemRoleType.SystemAdministrator))
        {
            return true;
        }

        // プロジェクトマネージャー以上の権限が必要
        return await _permissionService.HasProjectRoleAsync(userId, environment.ProjectId, ProjectRoleType.ProjectManager);
    }
}