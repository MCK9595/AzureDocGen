using AzureDocGen.Data.Entities;
using AzureDocGen.Data.Enums;
using AzureDocGen.Web.Models;

namespace AzureDocGen.Web.Services;

/// <summary>
/// プロジェクト管理サービスのインターフェース
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// プロジェクトを作成する
    /// </summary>
    Task<Project> CreateProjectAsync(string name, string description, string createdBy);
    
    /// <summary>
    /// プロジェクトをIDで取得する
    /// </summary>
    Task<Project?> GetProjectByIdAsync(Guid projectId);
    
    /// <summary>
    /// ユーザーがアクセス可能なプロジェクト一覧を取得する
    /// </summary>
    Task<List<Project>> GetUserProjectsAsync(string userId);
    
    /// <summary>
    /// ユーザーがアクセス可能なプロジェクト一覧を検索・フィルター・ページネーションで取得する
    /// </summary>
    Task<(List<Project> projects, int totalCount)> SearchUserProjectsAsync(string userId, ProjectSearchViewModel searchModel, int page, int pageSize);
    
    /// <summary>
    /// プロジェクトを更新する
    /// </summary>
    Task<Project> UpdateProjectAsync(Guid projectId, string name, string description);
    
    /// <summary>
    /// プロジェクトを削除する（ソフトデリート）
    /// </summary>
    Task<bool> DeleteProjectAsync(Guid projectId, string deletedBy);
    
    /// <summary>
    /// プロジェクトメンバーを追加する
    /// </summary>
    Task<bool> AddProjectMemberAsync(Guid projectId, string userId, ProjectRoleType roleType, string addedBy);
    
    /// <summary>
    /// プロジェクトメンバーを削除する
    /// </summary>
    Task<bool> RemoveProjectMemberAsync(Guid projectId, string userId, string removedBy);
    
    /// <summary>
    /// プロジェクトメンバーの権限を更新する
    /// </summary>
    Task<bool> UpdateProjectMemberRoleAsync(Guid projectId, string userId, ProjectRoleType newRoleType, string updatedBy);
    
    /// <summary>
    /// プロジェクトメンバー一覧を取得する
    /// </summary>
    Task<List<ProjectUserRole>> GetProjectMembersAsync(Guid projectId);
    
    /// <summary>
    /// プロジェクトに環境を追加する
    /// </summary>
    Task<Data.Entities.Environment> AddEnvironmentAsync(Guid projectId, string environmentName, string addedBy);
    
    /// <summary>
    /// プロジェクトの環境一覧を取得する
    /// </summary>
    Task<List<Data.Entities.Environment>> GetProjectEnvironmentsAsync(Guid projectId);
    
    /// <summary>
    /// 環境を削除する
    /// </summary>
    Task<bool> DeleteEnvironmentAsync(Guid environmentId, string deletedBy);
    
    /// <summary>
    /// プロジェクトの統計情報を取得する
    /// </summary>
    Task<ProjectStatistics> GetProjectStatisticsAsync(Guid projectId);
}

/// <summary>
/// プロジェクト統計情報
/// </summary>
public class ProjectStatistics
{
    public int MemberCount { get; set; }
    public int EnvironmentCount { get; set; }
    public int TemplateCount { get; set; }
    public int DocumentCount { get; set; }
    public DateTime? LastActivityDate { get; set; }
}