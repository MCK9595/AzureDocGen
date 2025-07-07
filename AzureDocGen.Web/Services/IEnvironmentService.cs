using AzureDocGen.Data.Entities;

namespace AzureDocGen.Web.Services;

/// <summary>
/// 環境管理サービスのインターフェース
/// </summary>
public interface IEnvironmentService
{
    /// <summary>
    /// 環境を作成する
    /// </summary>
    Task<AzureDocGen.Data.Entities.Environment> CreateEnvironmentAsync(Guid projectId, string name, string description, string createdBy);

    /// <summary>
    /// 環境をIDで取得する
    /// </summary>
    Task<AzureDocGen.Data.Entities.Environment?> GetEnvironmentByIdAsync(Guid environmentId);

    /// <summary>
    /// プロジェクトの環境一覧を取得する
    /// </summary>
    Task<List<AzureDocGen.Data.Entities.Environment>> GetProjectEnvironmentsAsync(Guid projectId);

    /// <summary>
    /// 環境を更新する
    /// </summary>
    Task<AzureDocGen.Data.Entities.Environment> UpdateEnvironmentAsync(Guid environmentId, string name, string description);

    /// <summary>
    /// 環境を削除する
    /// </summary>
    Task<bool> DeleteEnvironmentAsync(Guid environmentId, string deletedBy);

    /// <summary>
    /// 環境の順序を更新する
    /// </summary>
    Task<bool> UpdateEnvironmentOrderAsync(Guid projectId, List<Guid> orderedEnvironmentIds);

    /// <summary>
    /// 環境に設計書があるかチェックする
    /// </summary>
    Task<bool> HasDesignDocumentsAsync(Guid environmentId);

    /// <summary>
    /// ユーザーが環境にアクセスできるかチェックする
    /// </summary>
    Task<bool> CanUserAccessEnvironmentAsync(string userId, Guid environmentId);

    /// <summary>
    /// ユーザーが環境を編集できるかチェックする
    /// </summary>
    Task<bool> CanUserEditEnvironmentAsync(string userId, Guid environmentId);
}