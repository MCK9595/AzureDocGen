using System.ComponentModel.DataAnnotations;
using AzureDocGen.Data.Enums;

namespace AzureDocGen.Web.Models;

/// <summary>
/// プロジェクト一覧ビューモデル
/// </summary>
public class ProjectListViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public int EnvironmentCount { get; set; }
    public DateTime? LastActivityDate { get; set; }
    public ProjectRoleType? UserRole { get; set; }
}

/// <summary>
/// プロジェクト詳細ビューモデル
/// </summary>
public class ProjectDetailsViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public List<ProjectMemberViewModel> Members { get; set; } = new();
    public List<EnvironmentViewModel> Environments { get; set; } = new();
    public ProjectRoleType? CurrentUserRole { get; set; }
    public bool CanEdit { get; set; }
}

/// <summary>
/// プロジェクトメンバービューモデル
/// </summary>
public class ProjectMemberViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ProjectRoleType RoleType { get; set; }
    public DateTime GrantedAt { get; set; }
}

/// <summary>
/// 環境ビューモデル（プロジェクト詳細画面用）
/// </summary>
public class EnvironmentViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int DesignDocumentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

/// <summary>
/// プロジェクト作成ビューモデル
/// </summary>
public class ProjectCreateViewModel
{
    [Required(ErrorMessage = "プロジェクト名は必須です。")]
    [StringLength(200, ErrorMessage = "プロジェクト名は{1}文字以内で入力してください。")]
    [Display(Name = "プロジェクト名")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "説明は{1}文字以内で入力してください。")]
    [Display(Name = "説明")]
    public string? Description { get; set; }
}

/// <summary>
/// プロジェクト編集ビューモデル
/// </summary>
public class ProjectEditViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "プロジェクト名は必須です。")]
    [StringLength(200, ErrorMessage = "プロジェクト名は{1}文字以内で入力してください。")]
    [Display(Name = "プロジェクト名")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "説明は{1}文字以内で入力してください。")]
    [Display(Name = "説明")]
    public string? Description { get; set; }
}

/// <summary>
/// プロジェクト一覧ページングビューモデル
/// </summary>
public class ProjectIndexViewModel
{
    public List<ProjectListViewModel> Projects { get; set; } = new();
    public ProjectSearchViewModel SearchModel { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

/// <summary>
/// プロジェクト検索ビューモデル
/// </summary>
public class ProjectSearchViewModel
{
    [Display(Name = "検索キーワード")]
    public string? SearchTerm { get; set; }

    [Display(Name = "作成者")]
    public string? CreatedBy { get; set; }

    [Display(Name = "作成日開始")]
    [DataType(DataType.Date)]
    public DateTime? CreatedFromDate { get; set; }

    [Display(Name = "作成日終了")]
    [DataType(DataType.Date)]
    public DateTime? CreatedToDate { get; set; }

    [Display(Name = "自分の権限")]
    public ProjectRoleType? UserRole { get; set; }

    [Display(Name = "並び順")]
    public ProjectSortOrder SortOrder { get; set; } = ProjectSortOrder.CreatedDateDesc;
}

/// <summary>
/// プロジェクト並び順
/// </summary>
public enum ProjectSortOrder
{
    [Display(Name = "作成日（新しい順）")]
    CreatedDateDesc = 0,
    [Display(Name = "作成日（古い順）")]
    CreatedDateAsc = 1,
    [Display(Name = "名前（昇順）")]
    NameAsc = 2,
    [Display(Name = "名前（降順）")]
    NameDesc = 3,
    [Display(Name = "最終更新日（新しい順）")]
    LastActivityDesc = 4,
    [Display(Name = "最終更新日（古い順）")]
    LastActivityAsc = 5
}