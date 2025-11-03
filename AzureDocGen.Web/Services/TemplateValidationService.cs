using AzureDocGen.Data.Contexts;
using AzureDocGen.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AzureDocGen.Web.Services;

/// <summary>
/// テンプレート検証サービスの実装
/// </summary>
public class TemplateValidationService : ITemplateValidationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TemplateValidationService> _logger;

    // サポートされるパラメータータイプ
    private static readonly HashSet<string> SupportedParameterTypes = new()
    {
        "string", "int", "bool", "double", "datetime", "guid", "json"
    };

    // リソース名の命名規則（英数字、ハイフン、アンダースコア）
    private static readonly Regex ResourceNamePattern = new(@"^[a-zA-Z0-9\-_]+$", RegexOptions.Compiled);

    public TemplateValidationService(
        ApplicationDbContext context,
        ILogger<TemplateValidationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ValidationResult> ValidateTemplateStructureAsync(Dictionary<string, object> structure)
    {
        var result = new ValidationResult { IsValid = true };

        if (structure == null || structure.Count == 0)
        {
            result.AddError("テンプレート構造が空です。", "Structure");
            return result;
        }

        // 必須フィールドの検証
        if (!structure.ContainsKey("resources"))
        {
            result.AddError("テンプレート構造に 'resources' フィールドが必要です。", "Structure.resources");
        }

        // リソースの検証
        if (structure.ContainsKey("resources") && structure["resources"] is JsonElement resourcesElement)
        {
            try
            {
                if (resourcesElement.ValueKind == JsonValueKind.Array)
                {
                    var resourceNames = new HashSet<string>();
                    var resourceIndex = 0;

                    foreach (var resource in resourcesElement.EnumerateArray())
                    {
                        if (resource.TryGetProperty("name", out var nameElement))
                        {
                            var resourceName = nameElement.GetString();

                            if (string.IsNullOrWhiteSpace(resourceName))
                            {
                                result.AddError($"リソース[{resourceIndex}]の名前が空です。", $"Structure.resources[{resourceIndex}].name");
                            }
                            else
                            {
                                // 名前の重複チェック
                                if (resourceNames.Contains(resourceName))
                                {
                                    result.AddError($"リソース名 '{resourceName}' が重複しています。", $"Structure.resources[{resourceIndex}].name");
                                }
                                else
                                {
                                    resourceNames.Add(resourceName);
                                }

                                // 命名規則のチェック
                                if (!ResourceNamePattern.IsMatch(resourceName))
                                {
                                    result.AddError($"リソース名 '{resourceName}' が無効です。英数字、ハイフン、アンダースコアのみ使用できます。", $"Structure.resources[{resourceIndex}].name");
                                }
                            }
                        }
                        else
                        {
                            result.AddError($"リソース[{resourceIndex}]に 'name' フィールドがありません。", $"Structure.resources[{resourceIndex}]");
                        }

                        // リソースタイプの検証
                        if (!resource.TryGetProperty("type", out var typeElement) || string.IsNullOrWhiteSpace(typeElement.GetString()))
                        {
                            result.AddError($"リソース[{resourceIndex}]に 'type' フィールドがありません。", $"Structure.resources[{resourceIndex}].type");
                        }

                        resourceIndex++;
                    }
                }
                else
                {
                    result.AddError("'resources' フィールドは配列である必要があります。", "Structure.resources");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "テンプレート構造の検証中にエラーが発生しました");
                result.AddError($"テンプレート構造の解析中にエラーが発生しました: {ex.Message}", "Structure");
            }
        }

        // 接続の検証
        if (structure.ContainsKey("connections") && structure["connections"] is JsonElement connectionsElement)
        {
            try
            {
                if (connectionsElement.ValueKind == JsonValueKind.Array)
                {
                    var connectionIndex = 0;

                    foreach (var connection in connectionsElement.EnumerateArray())
                    {
                        if (!connection.TryGetProperty("source", out _))
                        {
                            result.AddError($"接続[{connectionIndex}]に 'source' フィールドがありません。", $"Structure.connections[{connectionIndex}].source");
                        }

                        if (!connection.TryGetProperty("target", out _))
                        {
                            result.AddError($"接続[{connectionIndex}]に 'target' フィールドがありません。", $"Structure.connections[{connectionIndex}].target");
                        }

                        connectionIndex++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "接続情報の検証中にエラーが発生しました");
                result.AddError($"接続情報の解析中にエラーが発生しました: {ex.Message}", "Structure.connections");
            }
        }

        return await Task.FromResult(result);
    }

    public async Task<ValidationResult> ValidateTemplateParametersAsync(Guid templateId, List<TemplateParameter> parameters)
    {
        var result = new ValidationResult { IsValid = true };

        if (parameters == null)
        {
            return result;
        }

        var parameterNames = new HashSet<string>();

        for (int i = 0; i < parameters.Count; i++)
        {
            var param = parameters[i];

            // 名前の検証
            if (string.IsNullOrWhiteSpace(param.Name))
            {
                result.AddError($"パラメーター[{i}]の名前が空です。", $"Parameters[{i}].Name");
            }
            else
            {
                // 名前の重複チェック
                if (parameterNames.Contains(param.Name))
                {
                    result.AddError($"パラメーター名 '{param.Name}' が重複しています。", $"Parameters[{i}].Name");
                }
                else
                {
                    parameterNames.Add(param.Name);
                }

                // 命名規則のチェック（パラメーター名は英数字とアンダースコアのみ）
                if (!Regex.IsMatch(param.Name, @"^[a-zA-Z][a-zA-Z0-9_]*$"))
                {
                    result.AddError($"パラメーター名 '{param.Name}' が無効です。英字で始まり、英数字とアンダースコアのみ使用できます。", $"Parameters[{i}].Name");
                }
            }

            // タイプの検証
            if (string.IsNullOrWhiteSpace(param.Type))
            {
                result.AddError($"パラメーター '{param.Name}' のタイプが指定されていません。", $"Parameters[{i}].Type");
            }
            else if (!SupportedParameterTypes.Contains(param.Type.ToLower()))
            {
                result.AddError($"パラメーター '{param.Name}' のタイプ '{param.Type}' はサポートされていません。", $"Parameters[{i}].Type");
            }

            // デフォルト値の検証
            if (!string.IsNullOrEmpty(param.DefaultValue))
            {
                var valueValidation = ValidateParameterValue(param, param.DefaultValue);
                if (!valueValidation.IsValid)
                {
                    result.AddError($"パラメーター '{param.Name}' のデフォルト値が無効です: {string.Join(", ", valueValidation.Errors.Select(e => e.Message))}", $"Parameters[{i}].DefaultValue");
                }
            }

            // 必須パラメーターの検証
            if (param.IsRequired && !string.IsNullOrEmpty(param.DefaultValue))
            {
                _logger.LogWarning("必須パラメーター '{ParameterName}' にデフォルト値が設定されています", param.Name);
            }
        }

        return await Task.FromResult(result);
    }

    public async Task<ValidationResult> ValidateTemplateAsync(Template template)
    {
        var result = new ValidationResult { IsValid = true };

        if (template == null)
        {
            result.AddError("テンプレートが null です。");
            return result;
        }

        // 基本情報の検証
        if (string.IsNullOrWhiteSpace(template.Name))
        {
            result.AddError("テンプレート名は必須です。", "Name");
        }
        else if (template.Name.Length > 200)
        {
            result.AddError("テンプレート名は200文字以内である必要があります。", "Name");
        }

        if (!string.IsNullOrWhiteSpace(template.Description) && template.Description.Length > 1000)
        {
            result.AddError("説明は1000文字以内である必要があります。", "Description");
        }

        // 構造の検証
        if (!string.IsNullOrEmpty(template.StructureJson))
        {
            try
            {
                var structure = JsonSerializer.Deserialize<Dictionary<string, object>>(template.StructureJson);
                if (structure != null)
                {
                    var structureValidation = await ValidateTemplateStructureAsync(structure);
                    if (!structureValidation.IsValid)
                    {
                        foreach (var error in structureValidation.Errors)
                        {
                            result.AddError(error.Message, error.PropertyName);
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "テンプレート構造のJSON解析中にエラーが発生しました");
                result.AddError($"テンプレート構造のJSON形式が無効です: {ex.Message}", "StructureJson");
            }
        }

        // パラメーターの検証
        if (template.Parameters != null && template.Parameters.Any())
        {
            var parameterValidation = await ValidateTemplateParametersAsync(template.Id, template.Parameters.ToList());
            if (!parameterValidation.IsValid)
            {
                foreach (var error in parameterValidation.Errors)
                {
                    result.AddError(error.Message, error.PropertyName);
                }
            }
        }

        // 同名テンプレートの存在チェック
        var existingTemplate = await _context.Templates
            .FirstOrDefaultAsync(t => t.Name == template.Name && t.Id != template.Id && t.CreatedBy == template.CreatedBy);

        if (existingTemplate != null)
        {
            result.AddError($"同じ名前のテンプレート '{template.Name}' が既に存在します。", "Name");
        }

        return result;
    }

    public ValidationResult ValidateParameterValue(TemplateParameter parameter, string? value)
    {
        var result = new ValidationResult { IsValid = true };

        // 必須チェック
        if (parameter.IsRequired && string.IsNullOrWhiteSpace(value))
        {
            result.AddError($"パラメーター '{parameter.Name}' は必須です。");
            return result;
        }

        // 値が空の場合はOK（必須でない場合）
        if (string.IsNullOrWhiteSpace(value))
        {
            return result;
        }

        // タイプ別の検証
        switch (parameter.Type?.ToLower())
        {
            case "int":
                if (!int.TryParse(value, out _))
                {
                    result.AddError($"'{value}' は整数ではありません。");
                }
                break;

            case "bool":
                if (!bool.TryParse(value, out _))
                {
                    result.AddError($"'{value}' はブール値ではありません。");
                }
                break;

            case "double":
                if (!double.TryParse(value, out _))
                {
                    result.AddError($"'{value}' は数値ではありません。");
                }
                break;

            case "datetime":
                if (!DateTime.TryParse(value, out _))
                {
                    result.AddError($"'{value}' は日時形式ではありません。");
                }
                break;

            case "guid":
                if (!Guid.TryParse(value, out _))
                {
                    result.AddError($"'{value}' はGUID形式ではありません。");
                }
                break;

            case "json":
                try
                {
                    JsonDocument.Parse(value);
                }
                catch (JsonException)
                {
                    result.AddError($"'{value}' は有効なJSON形式ではありません。");
                }
                break;

            case "string":
                // 文字列は常に有効
                break;

            default:
                _logger.LogWarning("未知のパラメータータイプ: {ParameterType}", parameter.Type);
                break;
        }

        return result;
    }
}
