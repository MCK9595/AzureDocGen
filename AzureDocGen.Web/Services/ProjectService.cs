using Microsoft.EntityFrameworkCore;
using AzureDocGen.Data.Contexts;
using AzureDocGen.Data.Entities;
using AzureDocGen.Data.Enums;
using AzureDocGen.Web.Models;

namespace AzureDocGen.Web.Services;

/// <summary>
/// プロジェクト管理サービスの実装
/// </summary>
public class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _context;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(
        ApplicationDbContext context,
        IPermissionService permissionService,
        ILogger<ProjectService> logger)
    {
        _context = context;
        _permissionService = permissionService;
        _logger = logger;
    }

    public async Task<Project> CreateProjectAsync(string name, string description, string createdBy)
    {
        // ユーザーが存在することを確認（存在しない場合は例外を投げる）
        var user = await _context.Users.FindAsync(createdBy);
        if (user == null)
        {
            _logger.LogError("User {UserId} not found in database when creating project", createdBy);
            throw new InvalidOperationException($"User {createdBy} not found. Please ensure you are properly logged in.");
        }

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        _context.Projects.Add(project);
        
        // 作成者をプロジェクトオーナーとして追加
        var ownerRole = new ProjectUserRole
        {
            Id = Guid.NewGuid(),
            UserId = createdBy,
            ProjectId = project.Id,
            RoleType = ProjectRoleType.ProjectOwner,
            GrantedAt = DateTime.UtcNow,
            GrantedBy = createdBy,
            IsActive = true
        };
        
        _context.ProjectUserRoles.Add(ownerRole);
        
        // デフォルト環境を作成
        var environments = new[] { 
            new { Name = "開発", Description = "開発環境" },
            new { Name = "検証", Description = "検証環境" },
            new { Name = "本番", Description = "本番環境" }
        };
        
        for (int i = 0; i < environments.Length; i++)
        {
            var env = environments[i];
            var environment = new Data.Entities.Environment
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                Name = env.Name,
                Description = env.Description,
                DisplayOrder = i + 1,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };
            _context.Environments.Add(environment);
        }
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Project {ProjectId} created by {UserId}", project.Id, createdBy);
        
        return project;
    }

    public async Task<Project?> GetProjectByIdAsync(Guid projectId)
    {
        return await _context.Projects
            .Include(p => p.Environments)
            .Include(p => p.NamingRules)
            .Include(p => p.Creator)
            .FirstOrDefaultAsync(p => p.Id == projectId);
    }

    public async Task<List<Project>> GetUserProjectsAsync(string userId)
    {
        // システム管理者の場合は全プロジェクトを返す
        if (await _permissionService.HasSystemRoleAsync(userId, SystemRoleType.SystemAdministrator))
        {
            return await _context.Projects
                .Include(p => p.Environments)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        // ユーザーが権限を持つプロジェクトIDを取得
        var accessibleProjectIds = await _permissionService.GetAccessibleProjectIdsAsync(userId);
        
        return await _context.Projects
            .Include(p => p.Environments)
            .Where(p => accessibleProjectIds.Contains(p.Id))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Project> UpdateProjectAsync(Guid projectId, string name, string description)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null)
        {
            throw new InvalidOperationException($"Project {projectId} not found");
        }

        project.Name = name;
        project.Description = description;
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Project {ProjectId} updated", projectId);
        
        return project;
    }

    public async Task<bool> DeleteProjectAsync(Guid projectId, string deletedBy)
    {
        var project = await _context.Projects
            .Include(p => p.Environments)
            .FirstOrDefaultAsync(p => p.Id == projectId);
            
        if (project == null)
        {
            return false;
        }

        // プロジェクトと関連データを削除（カスケード削除が設定されている）
        _context.Projects.Remove(project);
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Project {ProjectId} deleted by {UserId}", projectId, deletedBy);
        
        return true;
    }

    public async Task<bool> AddProjectMemberAsync(Guid projectId, string userId, ProjectRoleType roleType, string addedBy)
    {
        // 既存のロールを確認
        var existingRole = await _context.ProjectUserRoles
            .FirstOrDefaultAsync(pr => pr.ProjectId == projectId && pr.UserId == userId && pr.IsActive);
            
        if (existingRole != null)
        {
            // 既存のロールがある場合は更新
            existingRole.RoleType = roleType;
            existingRole.GrantedAt = DateTime.UtcNow;
            existingRole.GrantedBy = addedBy;
        }
        else
        {
            // 新規追加
            var projectRole = new ProjectUserRole
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProjectId = projectId,
                RoleType = roleType,
                GrantedAt = DateTime.UtcNow,
                GrantedBy = addedBy,
                IsActive = true
            };
            
            _context.ProjectUserRoles.Add(projectRole);
        }
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("User {UserId} added to project {ProjectId} with role {RoleType} by {AddedBy}", 
            userId, projectId, roleType, addedBy);
        
        return true;
    }

    public async Task<bool> RemoveProjectMemberAsync(Guid projectId, string userId, string removedBy)
    {
        var projectRole = await _context.ProjectUserRoles
            .FirstOrDefaultAsync(pr => pr.ProjectId == projectId && pr.UserId == userId && pr.IsActive);
            
        if (projectRole == null)
        {
            return false;
        }

        // ソフトデリート
        projectRole.IsActive = false;
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("User {UserId} removed from project {ProjectId} by {RemovedBy}", 
            userId, projectId, removedBy);
        
        return true;
    }

    public async Task<bool> UpdateProjectMemberRoleAsync(Guid projectId, string userId, ProjectRoleType newRoleType, string updatedBy)
    {
        var projectRole = await _context.ProjectUserRoles
            .FirstOrDefaultAsync(pr => pr.ProjectId == projectId && pr.UserId == userId && pr.IsActive);
            
        if (projectRole == null)
        {
            return false;
        }

        projectRole.RoleType = newRoleType;
        projectRole.GrantedAt = DateTime.UtcNow;
        projectRole.GrantedBy = updatedBy;
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("User {UserId} role updated to {RoleType} in project {ProjectId} by {UpdatedBy}", 
            userId, newRoleType, projectId, updatedBy);
        
        return true;
    }

    public async Task<List<ProjectUserRole>> GetProjectMembersAsync(Guid projectId)
    {
        return await _context.ProjectUserRoles
            .Include(pr => pr.User)
            .Where(pr => pr.ProjectId == projectId && pr.IsActive)
            .OrderBy(pr => pr.RoleType)
            .ThenBy(pr => pr.User!.LastName)
            .ThenBy(pr => pr.User!.FirstName)
            .ToListAsync();
    }

    public async Task<Data.Entities.Environment> AddEnvironmentAsync(Guid projectId, string environmentName, string addedBy)
    {
        var environment = new Data.Entities.Environment
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = environmentName
        };
        
        _context.Environments.Add(environment);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Environment {EnvironmentName} added to project {ProjectId} by {AddedBy}", 
            environmentName, projectId, addedBy);
        
        return environment;
    }

    public async Task<List<Data.Entities.Environment>> GetProjectEnvironmentsAsync(Guid projectId)
    {
        return await _context.Environments
            .Where(e => e.ProjectId == projectId)
            .OrderBy(e => e.Name)
            .ToListAsync();
    }

    public async Task<bool> DeleteEnvironmentAsync(Guid environmentId, string deletedBy)
    {
        var environment = await _context.Environments.FindAsync(environmentId);
        if (environment == null)
        {
            return false;
        }

        _context.Environments.Remove(environment);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Environment {EnvironmentId} deleted by {DeletedBy}", environmentId, deletedBy);
        
        return true;
    }

    public async Task<ProjectStatistics> GetProjectStatisticsAsync(Guid projectId)
    {
        var stats = new ProjectStatistics
        {
            MemberCount = await _context.ProjectUserRoles
                .CountAsync(pr => pr.ProjectId == projectId && pr.IsActive),
                
            EnvironmentCount = await _context.Environments
                .CountAsync(e => e.ProjectId == projectId),
                
            TemplateCount = await _context.Templates
                .CountAsync(t => t.SharingLevel == SharingLevel.Project || t.SharingLevel == SharingLevel.Global),
                
            DocumentCount = await _context.DesignDocuments
                .CountAsync(d => d.ProjectId == projectId)
        };

        // 最終アクティビティ日時を取得
        var lastDocument = await _context.DesignDocuments
            .Where(d => d.ProjectId == projectId)
            .OrderByDescending(d => d.CreatedAt)
            .FirstOrDefaultAsync();
            
        stats.LastActivityDate = lastDocument?.CreatedAt;
        
        return stats;
    }

    public async Task<(List<Project> projects, int totalCount)> SearchUserProjectsAsync(string userId, ProjectSearchViewModel searchModel, int page, int pageSize)
    {
        var query = _context.Projects
            .Include(p => p.Creator)
            .Include(p => p.ProjectUserRoles)
            .ThenInclude(pr => pr.User)
            .AsQueryable();

        // システム管理者またはグローバル閲覧者の場合は全プロジェクトにアクセス可能
        if (!await _permissionService.HasSystemRoleAsync(userId, SystemRoleType.SystemAdministrator) &&
            !await _permissionService.HasSystemRoleAsync(userId, SystemRoleType.GlobalViewer))
        {
            // 一般ユーザーの場合は、権限を持つプロジェクトのみ
            query = query.Where(p => p.ProjectUserRoles.Any(pr => 
                pr.UserId == userId && 
                pr.IsActive &&
                (pr.ExpiresAt == null || pr.ExpiresAt > DateTime.UtcNow)));
        }

        // 検索キーワードでフィルタリング
        if (!string.IsNullOrWhiteSpace(searchModel.SearchTerm))
        {
            var searchTerm = searchModel.SearchTerm.ToLower();
            query = query.Where(p => 
                p.Name.ToLower().Contains(searchTerm) || 
                p.Description.ToLower().Contains(searchTerm));
        }

        // 作成者でフィルタリング
        if (!string.IsNullOrWhiteSpace(searchModel.CreatedBy))
        {
            var createdBy = searchModel.CreatedBy.ToLower();
            query = query.Where(p => 
                p.Creator != null && 
                (p.Creator.Email.ToLower().Contains(createdBy) || 
                 p.Creator.FirstName.ToLower().Contains(createdBy) || 
                 p.Creator.LastName.ToLower().Contains(createdBy)));
        }

        // 作成日でフィルタリング
        if (searchModel.CreatedFromDate.HasValue)
        {
            var fromDate = searchModel.CreatedFromDate.Value.Date;
            query = query.Where(p => p.CreatedAt.Date >= fromDate);
        }

        if (searchModel.CreatedToDate.HasValue)
        {
            var toDate = searchModel.CreatedToDate.Value.Date.AddDays(1);
            query = query.Where(p => p.CreatedAt.Date < toDate);
        }

        // ユーザーの権限でフィルタリング
        if (searchModel.UserRole.HasValue)
        {
            query = query.Where(p => 
                p.ProjectUserRoles.Any(pr => 
                    pr.UserId == userId && 
                    pr.IsActive && 
                    pr.RoleType == searchModel.UserRole.Value));
        }

        // 総件数を取得
        var totalCount = await query.CountAsync();

        // ソート
        query = searchModel.SortOrder switch
        {
            ProjectSortOrder.CreatedDateAsc => query.OrderBy(p => p.CreatedAt),
            ProjectSortOrder.CreatedDateDesc => query.OrderByDescending(p => p.CreatedAt),
            ProjectSortOrder.NameAsc => query.OrderBy(p => p.Name),
            ProjectSortOrder.NameDesc => query.OrderByDescending(p => p.Name),
            ProjectSortOrder.LastActivityAsc => query.OrderBy(p => p.CreatedAt), // TODO: 実際の最終アクティビティ日時でソート
            ProjectSortOrder.LastActivityDesc => query.OrderByDescending(p => p.CreatedAt), // TODO: 実際の最終アクティビティ日時でソート
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        // ページネーション
        var projects = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (projects, totalCount);
    }
}