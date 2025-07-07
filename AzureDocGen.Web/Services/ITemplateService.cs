using AzureDocGen.Data.Entities;

namespace AzureDocGen.Web.Services;

/// <summary>
/// テンプレート管理サービスのインターフェース
/// </summary>
public interface ITemplateService
{
    /// <summary>
    /// テンプレートを作成する
    /// </summary>
    Task<Template> CreateTemplateAsync(string name, string description, SharingLevel sharingLevel, string createdBy);
    
    /// <summary>
    /// テンプレートをIDで取得する
    /// </summary>
    Task<Template?> GetTemplateByIdAsync(Guid templateId);
    
    /// <summary>
    /// ユーザーがアクセス可能なテンプレート一覧を取得する
    /// </summary>
    Task<List<Template>> GetUserTemplatesAsync(string userId, Guid? projectId = null);
    
    /// <summary>
    /// テンプレートを更新する
    /// </summary>
    Task<Template> UpdateTemplateAsync(Guid templateId, string name, string description, SharingLevel sharingLevel);
    
    /// <summary>
    /// テンプレートを削除する
    /// </summary>
    Task<bool> DeleteTemplateAsync(Guid templateId, string deletedBy);
    
    /// <summary>
    /// テンプレートにパラメーターを追加する
    /// </summary>
    Task<TemplateParameter> AddParameterAsync(Guid templateId, string parameterName, string parameterType, bool isRequired, string? defaultValue = null);
    
    /// <summary>
    /// テンプレートパラメーターを更新する
    /// </summary>
    Task<TemplateParameter> UpdateParameterAsync(Guid parameterId, string parameterName, string parameterType, bool isRequired, string? defaultValue = null);
    
    /// <summary>
    /// テンプレートパラメーターを削除する
    /// </summary>
    Task<bool> DeleteParameterAsync(Guid parameterId);
    
    /// <summary>
    /// テンプレートのパラメーター一覧を取得する
    /// </summary>
    Task<List<TemplateParameter>> GetTemplateParametersAsync(Guid templateId);
    
    /// <summary>
    /// テンプレートの新しいバージョンを作成する
    /// </summary>
    Task<Template> CreateVersionAsync(Guid baseTemplateId, string createdBy);
    
    /// <summary>
    /// テンプレートのバージョン履歴を取得する
    /// </summary>
    Task<List<Template>> GetTemplateVersionsAsync(string templateName, string createdBy);
    
    /// <summary>
    /// テンプレート構造を更新する
    /// </summary>
    Task<Template> UpdateTemplateStructureAsync(Guid templateId, Dictionary<string, object> structure);
    
    /// <summary>
    /// テンプレートを複製する
    /// </summary>
    Task<Template> DuplicateTemplateAsync(Guid templateId, string newName, string createdBy);
    
    /// <summary>
    /// カテゴリー別テンプレート統計を取得する
    /// </summary>
    Task<TemplateStatistics> GetTemplateStatisticsAsync(string userId);
}

/// <summary>
/// テンプレート統計情報
/// </summary>
public class TemplateStatistics
{
    public int TotalTemplates { get; set; }
    public int PrivateTemplates { get; set; }
    public int ProjectTemplates { get; set; }
    public int GlobalTemplates { get; set; }
    public int RecentlyUsedCount { get; set; }
    public DateTime? LastCreatedDate { get; set; }
}