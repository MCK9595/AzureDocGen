using AzureDocGen.Data.Entities;

namespace AzureDocGen.Web.Services;

/// <summary>
/// リソース管理サービスのインターフェース
/// </summary>
public interface IResourceService
{
    /// <summary>
    /// リソースを作成する
    /// </summary>
    Task<Resource> CreateResourceAsync(Guid environmentId, string resourceType, string name, Position position);

    /// <summary>
    /// リソースをIDで取得する
    /// </summary>
    Task<Resource?> GetResourceByIdAsync(Guid resourceId);

    /// <summary>
    /// 環境のリソース一覧を取得する
    /// </summary>
    Task<List<Resource>> GetEnvironmentResourcesAsync(Guid environmentId);

    /// <summary>
    /// リソースを更新する
    /// </summary>
    Task<Resource> UpdateResourceAsync(Guid resourceId, string? name = null, Dictionary<string, object>? properties = null);

    /// <summary>
    /// リソースの位置を更新する
    /// </summary>
    Task<Resource> UpdateResourcePositionAsync(Guid resourceId, Position position);

    /// <summary>
    /// リソースを削除する
    /// </summary>
    Task<bool> DeleteResourceAsync(Guid resourceId);

    /// <summary>
    /// リソース間の接続を作成する
    /// </summary>
    Task<ResourceConnection> CreateConnectionAsync(Guid sourceResourceId, Guid targetResourceId, string connectionType);

    /// <summary>
    /// リソース間の接続を取得する
    /// </summary>
    Task<List<ResourceConnection>> GetResourceConnectionsAsync(Guid resourceId);

    /// <summary>
    /// 環境内のすべての接続を取得する
    /// </summary>
    Task<List<ResourceConnection>> GetEnvironmentConnectionsAsync(Guid environmentId);

    /// <summary>
    /// 接続を削除する
    /// </summary>
    Task<bool> DeleteConnectionAsync(Guid connectionId);

    /// <summary>
    /// 環境のリソースをバッチで作成する（テンプレートから）
    /// </summary>
    Task<List<Resource>> CreateResourcesFromTemplateAsync(Guid environmentId, Template template, Dictionary<string, string>? parameterValues = null);
}
