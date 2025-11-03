using AzureDocGen.Data.Entities;

namespace AzureDocGen.Web.Services;

/// <summary>
/// テンプレート検証サービスのインターフェース
/// </summary>
public interface ITemplateValidationService
{
    /// <summary>
    /// テンプレート構造を検証する
    /// </summary>
    Task<ValidationResult> ValidateTemplateStructureAsync(Dictionary<string, object> structure);

    /// <summary>
    /// テンプレートパラメーターの整合性を検証する
    /// </summary>
    Task<ValidationResult> ValidateTemplateParametersAsync(Guid templateId, List<TemplateParameter> parameters);

    /// <summary>
    /// テンプレート全体の検証を実行する
    /// </summary>
    Task<ValidationResult> ValidateTemplateAsync(Template template);

    /// <summary>
    /// パラメーター値が正しいかを検証する
    /// </summary>
    ValidationResult ValidateParameterValue(TemplateParameter parameter, string? value);
}

/// <summary>
/// 検証結果
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; } = new();

    public static ValidationResult Success() => new ValidationResult { IsValid = true };

    public static ValidationResult Failure(string message, string? propertyName = null)
    {
        return new ValidationResult
        {
            IsValid = false,
            Errors = new List<ValidationError>
            {
                new ValidationError { Message = message, PropertyName = propertyName }
            }
        };
    }

    public void AddError(string message, string? propertyName = null)
    {
        IsValid = false;
        Errors.Add(new ValidationError { Message = message, PropertyName = propertyName });
    }
}

/// <summary>
/// 検証エラー
/// </summary>
public class ValidationError
{
    public string Message { get; set; } = string.Empty;
    public string? PropertyName { get; set; }
    public string? ErrorCode { get; set; }
}
