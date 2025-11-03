using AzureDocGen.Data.Entities;

namespace AzureDocGen.Web.Services;

/// <summary>
/// 命名規則サービスのインターフェース
/// </summary>
public interface INamingRuleService
{
    /// <summary>
    /// 命名規則を作成
    /// </summary>
    Task<NamingRule> CreateNamingRuleAsync(Guid projectId, string resourceType, string pattern,
        string description, string createdBy);

    /// <summary>
    /// 命名規則を取得
    /// </summary>
    Task<NamingRule?> GetNamingRuleByIdAsync(Guid id);

    /// <summary>
    /// プロジェクトの命名規則一覧を取得
    /// </summary>
    Task<List<NamingRule>> GetProjectNamingRulesAsync(Guid projectId);

    /// <summary>
    /// リソースタイプ別の命名規則を取得
    /// </summary>
    Task<NamingRule?> GetNamingRuleByResourceTypeAsync(Guid projectId, string resourceType);

    /// <summary>
    /// 命名規則を更新
    /// </summary>
    Task<NamingRule> UpdateNamingRuleAsync(Guid id, string pattern, string description);

    /// <summary>
    /// 命名規則を削除
    /// </summary>
    Task<bool> DeleteNamingRuleAsync(Guid id);

    /// <summary>
    /// 命名規則に基づいてリソース名を生成
    /// </summary>
    string GenerateResourceName(NamingRule rule, Dictionary<string, string> parameters);

    /// <summary>
    /// リソース名が命名規則に準拠しているか検証
    /// </summary>
    Task<(bool isValid, string? errorMessage)> ValidateResourceNameAsync(
        Guid projectId, string resourceType, string resourceName);

    /// <summary>
    /// 命名規則のプレビューを生成
    /// </summary>
    string PreviewNamingRule(string pattern, Dictionary<string, string> sampleParameters);
}
