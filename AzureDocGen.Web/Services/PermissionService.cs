using Microsoft.EntityFrameworkCore;
using AzureDocGen.Data.Contexts;
using AzureDocGen.Data.Entities;
using AzureDocGen.Data.Enums;

namespace AzureDocGen.Web.Services;

/// <summary>
/// 権限チェックサービスの実装
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(ApplicationDbContext context, ILogger<PermissionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> HasSystemRoleAsync(string userId, SystemRoleType roleType)
    {
        var result = await _context.SystemRoles
            .AnyAsync(sr => sr.UserId == userId && sr.RoleType == roleType && sr.IsActive);
        
        _logger.LogDebug("HasSystemRoleAsync: userId={UserId}, roleType={RoleType}, result={Result}", userId, roleType, result);
        
        return result;
    }

    public async Task<bool> HasProjectRoleAsync(string userId, Guid projectId, ProjectRoleType roleType)
    {
        // システム管理者は全権限を持つ
        if (await HasSystemRoleAsync(userId, SystemRoleType.SystemAdministrator))
            return true;

        return await _context.ProjectUserRoles
            .AnyAsync(pr => pr.UserId == userId && pr.ProjectId == projectId && 
                           pr.RoleType == roleType && pr.IsActive &&
                           (pr.ExpiresAt == null || pr.ExpiresAt > DateTime.UtcNow));
    }

    public async Task<bool> HasProjectRoleOrHigherAsync(string userId, Guid projectId, ProjectRoleType minimumRole)
    {
        // システム管理者は全権限を持つ
        if (await HasSystemRoleAsync(userId, SystemRoleType.SystemAdministrator))
            return true;

        var userRole = await GetProjectRoleAsync(userId, projectId);
        if (userRole == null)
            return false;

        // 権限の階層: Owner > Manager > Reviewer > Developer > Viewer
        return userRole.Value switch
        {
            ProjectRoleType.ProjectOwner => true,
            ProjectRoleType.ProjectManager => minimumRole != ProjectRoleType.ProjectOwner,
            ProjectRoleType.ProjectReviewer => minimumRole == ProjectRoleType.ProjectReviewer || 
                                              minimumRole == ProjectRoleType.ProjectDeveloper || 
                                              minimumRole == ProjectRoleType.ProjectViewer,
            ProjectRoleType.ProjectDeveloper => minimumRole == ProjectRoleType.ProjectDeveloper || 
                                               minimumRole == ProjectRoleType.ProjectViewer,
            ProjectRoleType.ProjectViewer => minimumRole == ProjectRoleType.ProjectViewer,
            _ => false
        };
    }

    public async Task<bool> HasEnvironmentRoleAsync(string userId, Guid environmentId, EnvironmentRoleType roleType)
    {
        // システム管理者は全権限を持つ
        if (await HasSystemRoleAsync(userId, SystemRoleType.SystemAdministrator))
            return true;

        // 環境が属するプロジェクトの権限もチェック
        var environment = await _context.Environments
            .FirstOrDefaultAsync(e => e.Id == environmentId);
        
        if (environment != null)
        {
            // プロジェクトオーナー/マネージャーは環境に対する全権限を持つ
            if (await HasProjectRoleOrHigherAsync(userId, environment.ProjectId, ProjectRoleType.ProjectManager))
                return true;
        }

        return await _context.EnvironmentUserRoles
            .AnyAsync(er => er.UserId == userId && er.EnvironmentId == environmentId && 
                           er.RoleType == roleType && er.IsActive &&
                           (er.ExpiresAt == null || er.ExpiresAt > DateTime.UtcNow));
    }

    public async Task<bool> CanAccessProjectAsync(string userId, Guid projectId)
    {
        // システム管理者またはグローバル閲覧者はアクセス可能
        if (await HasSystemRoleAsync(userId, SystemRoleType.SystemAdministrator) ||
            await HasSystemRoleAsync(userId, SystemRoleType.GlobalViewer))
            return true;

        // プロジェクトに対して何らかの権限を持っているか
        return await _context.ProjectUserRoles
            .AnyAsync(pr => pr.UserId == userId && pr.ProjectId == projectId && pr.IsActive &&
                           (pr.ExpiresAt == null || pr.ExpiresAt > DateTime.UtcNow));
    }

    public async Task<bool> CanAccessEnvironmentAsync(string userId, Guid environmentId)
    {
        // システム管理者またはグローバル閲覧者はアクセス可能
        if (await HasSystemRoleAsync(userId, SystemRoleType.SystemAdministrator) ||
            await HasSystemRoleAsync(userId, SystemRoleType.GlobalViewer))
            return true;

        var environment = await _context.Environments
            .FirstOrDefaultAsync(e => e.Id == environmentId);
        
        if (environment == null)
            return false;

        // プロジェクトレベルでアクセス権があるか
        if (await CanAccessProjectAsync(userId, environment.ProjectId))
            return true;

        // 環境レベルでアクセス権があるか
        return await _context.EnvironmentUserRoles
            .AnyAsync(er => er.UserId == userId && er.EnvironmentId == environmentId && er.IsActive &&
                           (er.ExpiresAt == null || er.ExpiresAt > DateTime.UtcNow));
    }

    public async Task<bool> CanReviewDocumentAsync(string userId, Guid documentId)
    {
        // レビューワークフローでレビューアーとして割り当てられているか
        var isAssignedReviewer = await _context.ReviewAssignments
            .AnyAsync(ra => ra.ReviewerId == userId && 
                           ra.Workflow!.TargetId == documentId &&
                           ra.Workflow.TargetType == ReviewTargetType.DesignDocument &&
                           ra.Status == ReviewAssignmentStatus.Pending);

        if (isAssignedReviewer)
            return true;

        // ドキュメントが属するプロジェクトでレビューアー以上の権限を持っているか
        var document = await _context.DesignDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId);
        
        if (document != null)
        {
            return await HasProjectRoleOrHigherAsync(userId, document.ProjectId, ProjectRoleType.ProjectReviewer);
        }

        return false;
    }

    public async Task<ProjectRoleType?> GetProjectRoleAsync(string userId, Guid projectId)
    {
        var role = await _context.ProjectUserRoles
            .Where(pr => pr.UserId == userId && pr.ProjectId == projectId && pr.IsActive &&
                        (pr.ExpiresAt == null || pr.ExpiresAt > DateTime.UtcNow))
            .Select(pr => pr.RoleType)
            .FirstOrDefaultAsync();

        return role == default ? null : role;
    }

    public async Task<EnvironmentRoleType?> GetEnvironmentRoleAsync(string userId, Guid environmentId)
    {
        var role = await _context.EnvironmentUserRoles
            .Where(er => er.UserId == userId && er.EnvironmentId == environmentId && er.IsActive &&
                        (er.ExpiresAt == null || er.ExpiresAt > DateTime.UtcNow))
            .Select(er => er.RoleType)
            .FirstOrDefaultAsync();

        return role == default ? null : role;
    }

    public async Task<List<Guid>> GetAccessibleProjectIdsAsync(string userId)
    {
        // システム管理者またはグローバル閲覧者は全プロジェクトにアクセス可能
        if (await HasSystemRoleAsync(userId, SystemRoleType.SystemAdministrator) ||
            await HasSystemRoleAsync(userId, SystemRoleType.GlobalViewer))
        {
            return await _context.Projects.Select(p => p.Id).ToListAsync();
        }

        // ユーザーが権限を持つプロジェクトのIDを取得
        return await _context.ProjectUserRoles
            .Where(pr => pr.UserId == userId && pr.IsActive &&
                        (pr.ExpiresAt == null || pr.ExpiresAt > DateTime.UtcNow))
            .Select(pr => pr.ProjectId)
            .Distinct()
            .ToListAsync();
    }

    public async Task GrantProjectRoleAsync(string userId, Guid projectId, ProjectRoleType roleType, string grantedBy)
    {
        // 既存の権限を無効化
        var existingRoles = await _context.ProjectUserRoles
            .Where(pr => pr.UserId == userId && pr.ProjectId == projectId && pr.IsActive)
            .ToListAsync();

        foreach (var existingRole in existingRoles)
        {
            existingRole.IsActive = false;
        }

        // 新しい権限を付与
        var newRole = new ProjectUserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProjectId = projectId,
            RoleType = roleType,
            GrantedAt = DateTime.UtcNow,
            GrantedBy = grantedBy,
            IsActive = true
        };

        _context.ProjectUserRoles.Add(newRole);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Project role {RoleType} granted to user {UserId} for project {ProjectId} by {GrantedBy}",
            roleType, userId, projectId, grantedBy);
    }

    public async Task GrantEnvironmentRoleAsync(string userId, Guid environmentId, EnvironmentRoleType roleType, string grantedBy)
    {
        // 既存の権限を無効化
        var existingRoles = await _context.EnvironmentUserRoles
            .Where(er => er.UserId == userId && er.EnvironmentId == environmentId && er.IsActive)
            .ToListAsync();

        foreach (var existingRole in existingRoles)
        {
            existingRole.IsActive = false;
        }

        // 新しい権限を付与
        var newRole = new EnvironmentUserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EnvironmentId = environmentId,
            RoleType = roleType,
            GrantedAt = DateTime.UtcNow,
            GrantedBy = grantedBy,
            IsActive = true
        };

        _context.EnvironmentUserRoles.Add(newRole);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Environment role {RoleType} granted to user {UserId} for environment {EnvironmentId} by {GrantedBy}",
            roleType, userId, environmentId, grantedBy);
    }

    public async Task RevokeProjectRoleAsync(string userId, Guid projectId)
    {
        var roles = await _context.ProjectUserRoles
            .Where(pr => pr.UserId == userId && pr.ProjectId == projectId && pr.IsActive)
            .ToListAsync();

        foreach (var role in roles)
        {
            role.IsActive = false;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Project roles revoked for user {UserId} on project {ProjectId}", userId, projectId);
    }

    public async Task RevokeEnvironmentRoleAsync(string userId, Guid environmentId)
    {
        var roles = await _context.EnvironmentUserRoles
            .Where(er => er.UserId == userId && er.EnvironmentId == environmentId && er.IsActive)
            .ToListAsync();

        foreach (var role in roles)
        {
            role.IsActive = false;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Environment roles revoked for user {UserId} on environment {EnvironmentId}", userId, environmentId);
    }
}