using AzureDocGen.Data.Enums;

namespace AzureDocGen.Web.Services;

/// <summary>
/// 権限チェックサービスのインターフェース
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// ユーザーがシステム権限を持っているかチェック
    /// </summary>
    Task<bool> HasSystemRoleAsync(string userId, SystemRoleType roleType);
    
    /// <summary>
    /// ユーザーがプロジェクト権限を持っているかチェック
    /// </summary>
    Task<bool> HasProjectRoleAsync(string userId, Guid projectId, ProjectRoleType roleType);
    
    /// <summary>
    /// ユーザーがプロジェクトに対して指定された権限以上を持っているかチェック
    /// </summary>
    Task<bool> HasProjectRoleOrHigherAsync(string userId, Guid projectId, ProjectRoleType minimumRole);
    
    /// <summary>
    /// ユーザーが環境権限を持っているかチェック
    /// </summary>
    Task<bool> HasEnvironmentRoleAsync(string userId, Guid environmentId, EnvironmentRoleType roleType);
    
    /// <summary>
    /// ユーザーがプロジェクトにアクセスできるかチェック（任意の権限）
    /// </summary>
    Task<bool> CanAccessProjectAsync(string userId, Guid projectId);
    
    /// <summary>
    /// ユーザーが環境にアクセスできるかチェック（任意の権限）
    /// </summary>
    Task<bool> CanAccessEnvironmentAsync(string userId, Guid environmentId);
    
    /// <summary>
    /// ユーザーがドキュメントをレビューできるかチェック
    /// </summary>
    Task<bool> CanReviewDocumentAsync(string userId, Guid documentId);
    
    /// <summary>
    /// ユーザーのプロジェクト権限を取得
    /// </summary>
    Task<ProjectRoleType?> GetProjectRoleAsync(string userId, Guid projectId);
    
    /// <summary>
    /// ユーザーの環境権限を取得
    /// </summary>
    Task<EnvironmentRoleType?> GetEnvironmentRoleAsync(string userId, Guid environmentId);
    
    /// <summary>
    /// ユーザーがアクセスできるプロジェクトのリストを取得
    /// </summary>
    Task<List<Guid>> GetAccessibleProjectIdsAsync(string userId);
    
    /// <summary>
    /// プロジェクトに権限を付与
    /// </summary>
    Task GrantProjectRoleAsync(string userId, Guid projectId, ProjectRoleType roleType, string grantedBy);
    
    /// <summary>
    /// 環境に権限を付与
    /// </summary>
    Task GrantEnvironmentRoleAsync(string userId, Guid environmentId, EnvironmentRoleType roleType, string grantedBy);
    
    /// <summary>
    /// プロジェクト権限を取り消し
    /// </summary>
    Task RevokeProjectRoleAsync(string userId, Guid projectId);
    
    /// <summary>
    /// 環境権限を取り消し
    /// </summary>
    Task RevokeEnvironmentRoleAsync(string userId, Guid environmentId);
}