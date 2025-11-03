using Microsoft.EntityFrameworkCore;
using AzureDocGen.Data.Contexts;
using AzureDocGen.Data.Entities;
using System.Text.RegularExpressions;

namespace AzureDocGen.Web.Services;

/// <summary>
/// 命名規則サービスの実装
/// </summary>
public class NamingRuleService : INamingRuleService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NamingRuleService> _logger;

    public NamingRuleService(
        ApplicationDbContext context,
        ILogger<NamingRuleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<NamingRule> CreateNamingRuleAsync(Guid projectId, string resourceType,
        string pattern, string description, string createdBy)
    {
        // 既存の命名規則をチェック
        var existing = await _context.NamingRules
            .FirstOrDefaultAsync(nr => nr.ProjectId == projectId && nr.ResourceType == resourceType);

        if (existing != null)
        {
            throw new InvalidOperationException($"このプロジェクトには既にリソースタイプ '{resourceType}' の命名規則が存在します。");
        }

        var namingRule = new NamingRule
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            ResourceType = resourceType,
            Pattern = pattern,
            Description = description,
            IsActive = true,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.NamingRules.Add(namingRule);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Naming rule created for resource type {ResourceType} in project {ProjectId}",
            resourceType, projectId);

        return namingRule;
    }

    public async Task<NamingRule?> GetNamingRuleByIdAsync(Guid id)
    {
        return await _context.NamingRules
            .Include(nr => nr.Project)
            .FirstOrDefaultAsync(nr => nr.Id == id);
    }

    public async Task<List<NamingRule>> GetProjectNamingRulesAsync(Guid projectId)
    {
        return await _context.NamingRules
            .Where(nr => nr.ProjectId == projectId)
            .OrderBy(nr => nr.ResourceType)
            .ToListAsync();
    }

    public async Task<NamingRule?> GetNamingRuleByResourceTypeAsync(Guid projectId, string resourceType)
    {
        return await _context.NamingRules
            .FirstOrDefaultAsync(nr => nr.ProjectId == projectId &&
                                     nr.ResourceType == resourceType &&
                                     nr.IsActive);
    }

    public async Task<NamingRule> UpdateNamingRuleAsync(Guid id, string pattern, string description)
    {
        var namingRule = await _context.NamingRules.FindAsync(id);
        if (namingRule == null)
        {
            throw new InvalidOperationException($"命名規則 {id} が見つかりません。");
        }

        namingRule.Pattern = pattern;
        namingRule.Description = description;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Naming rule {Id} updated", id);

        return namingRule;
    }

    public async Task<bool> DeleteNamingRuleAsync(Guid id)
    {
        var namingRule = await _context.NamingRules.FindAsync(id);
        if (namingRule == null)
        {
            return false;
        }

        _context.NamingRules.Remove(namingRule);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Naming rule {Id} deleted", id);

        return true;
    }

    public string GenerateResourceName(NamingRule rule, Dictionary<string, string> parameters)
    {
        var result = rule.Pattern;

        // プレースホルダーを置換 {key} → value
        foreach (var param in parameters)
        {
            var placeholder = $"{{{param.Key}}}";
            result = result.Replace(placeholder, param.Value);
        }

        // 未置換のプレースホルダーをチェック
        var remainingPlaceholders = Regex.Matches(result, @"\{([^}]+)\}");
        if (remainingPlaceholders.Count > 0)
        {
            _logger.LogWarning("命名規則に未置換のプレースホルダーが残っています: {Pattern}", result);
        }

        return result;
    }

    public async Task<(bool isValid, string? errorMessage)> ValidateResourceNameAsync(
        Guid projectId, string resourceType, string resourceName)
    {
        var rule = await GetNamingRuleByResourceTypeAsync(projectId, resourceType);

        if (rule == null)
        {
            // 命名規則が設定されていない場合は常に有効
            return (true, null);
        }

        // パターンを正規表現に変換
        var regexPattern = ConvertPatternToRegex(rule.Pattern);

        try
        {
            var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
            var isMatch = regex.IsMatch(resourceName);

            if (!isMatch)
            {
                return (false, $"リソース名が命名規則 '{rule.Pattern}' に準拠していません。");
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "命名規則のバリデーション中にエラーが発生しました");
            return (false, $"命名規則の検証エラー: {ex.Message}");
        }
    }

    public string PreviewNamingRule(string pattern, Dictionary<string, string> sampleParameters)
    {
        var result = pattern;

        foreach (var param in sampleParameters)
        {
            var placeholder = $"{{{param.Key}}}";
            result = result.Replace(placeholder, param.Value);
        }

        return result;
    }

    /// <summary>
    /// 命名規則パターンを正規表現に変換
    /// </summary>
    private string ConvertPatternToRegex(string pattern)
    {
        // プレースホルダー {xxx} を正規表現グループに変換
        var regexPattern = Regex.Escape(pattern);

        // {xxx} → (.+) に変換
        regexPattern = Regex.Replace(regexPattern, @"\\\{[^}]+\\\}", "(.+)");

        // 完全一致を要求
        return $"^{regexPattern}$";
    }
}
