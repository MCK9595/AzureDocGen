using System.ComponentModel.DataAnnotations;
using AzureDocGen.Data.Entities;
using AzureDocGen.Web.Services;

namespace AzureDocGen.Web.Models;

/// <summary>
/// テンプレート一覧ビューモデル
/// </summary>
public class TemplateIndexViewModel
{
    public List<TemplateListViewModel> Templates { get; set; } = new();
    public TemplateStatistics Statistics { get; set; } = new();
    public string? CurrentCategory { get; set; }
    public TemplateSearchViewModel SearchModel { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

/// <summary>
/// テンプレート一覧項目ビューモデル
/// </summary>
public class TemplateListViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SharingLevel SharingLevel { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public int ParameterCount { get; set; }
    public bool IsOwner { get; set; }
}

/// <summary>
/// テンプレート詳細ビューモデル
/// </summary>
public class TemplateDetailsViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SharingLevel SharingLevel { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public List<TemplateParameterViewModel> Parameters { get; set; } = new();
    public List<TemplateVersionViewModel> Versions { get; set; } = new();
    public Dictionary<string, object>? Structure { get; set; }
    public bool IsOwner { get; set; }
    public bool CanEdit { get; set; }
}

/// <summary>
/// テンプレートパラメータービューモデル
/// </summary>
public class TemplateParameterViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ParameterType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
}

/// <summary>
/// テンプレートバージョンビューモデル
/// </summary>
public class TemplateVersionViewModel
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// テンプレート作成ビューモデル
/// </summary>
public class TemplateCreateViewModel
{
    [Required(ErrorMessage = "テンプレート名は必須です。")]
    [StringLength(200, ErrorMessage = "テンプレート名は{1}文字以内で入力してください。")]
    [Display(Name = "テンプレート名")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "説明は{1}文字以内で入力してください。")]
    [Display(Name = "説明")]
    public string? Description { get; set; }

    [Display(Name = "共有レベル")]
    public SharingLevel SharingLevel { get; set; } = SharingLevel.Private;
}

/// <summary>
/// テンプレート編集ビューモデル
/// </summary>
public class TemplateEditViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "テンプレート名は必須です。")]
    [StringLength(200, ErrorMessage = "テンプレート名は{1}文字以内で入力してください。")]
    [Display(Name = "テンプレート名")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "説明は{1}文字以内で入力してください。")]
    [Display(Name = "説明")]
    public string? Description { get; set; }

    [Display(Name = "共有レベル")]
    public SharingLevel SharingLevel { get; set; }
}

/// <summary>
/// パラメーター追加ビューモデル
/// </summary>
public class ParameterAddViewModel
{
    public Guid TemplateId { get; set; }

    [Required(ErrorMessage = "パラメーター名は必須です。")]
    [StringLength(100, ErrorMessage = "パラメーター名は{1}文字以内で入力してください。")]
    [Display(Name = "パラメーター名")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "パラメータータイプは必須です。")]
    [Display(Name = "パラメータータイプ")]
    public string ParameterType { get; set; } = "string";

    [Display(Name = "必須項目")]
    public bool IsRequired { get; set; }

    [StringLength(500, ErrorMessage = "デフォルト値は{1}文字以内で入力してください。")]
    [Display(Name = "デフォルト値")]
    public string? DefaultValue { get; set; }
}

/// <summary>
/// テンプレート検索ビューモデル
/// </summary>
public class TemplateSearchViewModel
{
    [Display(Name = "検索キーワード")]
    public string? SearchTerm { get; set; }

    [Display(Name = "共有レベル")]
    public SharingLevel? SharingLevel { get; set; }

    [Display(Name = "作成者")]
    public string? CreatedBy { get; set; }

    [Display(Name = "作成日開始")]
    [DataType(DataType.Date)]
    public DateTime? CreatedFromDate { get; set; }

    [Display(Name = "作成日終了")]
    [DataType(DataType.Date)]
    public DateTime? CreatedToDate { get; set; }

    [Display(Name = "並び順")]
    public TemplateSortOrder SortOrder { get; set; } = TemplateSortOrder.CreatedDateDesc;
}

/// <summary>
/// テンプレート並び順
/// </summary>
public enum TemplateSortOrder
{
    [Display(Name = "作成日（新しい順）")]
    CreatedDateDesc = 0,
    [Display(Name = "作成日（古い順）")]
    CreatedDateAsc = 1,
    [Display(Name = "名前（昇順）")]
    NameAsc = 2,
    [Display(Name = "名前（降順）")]
    NameDesc = 3,
    [Display(Name = "バージョン（新しい順）")]
    VersionDesc = 4,
    [Display(Name = "バージョン（古い順）")]
    VersionAsc = 5
}