using System.ComponentModel.DataAnnotations;

namespace AzureDocGen.Web.Models;

/// <summary>
/// 環境一覧表示用ViewModel
/// </summary>
public class EnvironmentListViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public bool HasDesignDocuments { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

/// <summary>
/// 環境詳細表示用ViewModel
/// </summary>
public class EnvironmentDetailsViewModel
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public bool HasDesignDocuments { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public List<DesignDocumentSummary> DesignDocuments { get; set; } = new();
}

/// <summary>
/// 設計書サマリー（環境詳細画面用）
/// </summary>
public class DesignDocumentSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
    public string LastModifiedBy { get; set; } = string.Empty;
}

/// <summary>
/// 環境作成用ViewModel
/// </summary>
public class EnvironmentCreateViewModel
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;

    [Required(ErrorMessage = "環境名は必須です")]
    [StringLength(100, ErrorMessage = "環境名は100文字以内で入力してください")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "説明は500文字以内で入力してください")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 環境編集用ViewModel
/// </summary>
public class EnvironmentEditViewModel
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;

    [Required(ErrorMessage = "環境名は必須です")]
    [StringLength(100, ErrorMessage = "環境名は100文字以内で入力してください")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "説明は500文字以内で入力してください")]
    public string Description { get; set; } = string.Empty;

    public bool HasDesignDocuments { get; set; }
}

/// <summary>
/// 環境順序更新用ViewModel
/// </summary>
public class EnvironmentOrderUpdateViewModel
{
    public Guid ProjectId { get; set; }
    public List<Guid> OrderedEnvironmentIds { get; set; } = new();
}

/// <summary>
/// プロジェクト環境管理用ViewModel（プロジェクト詳細画面用）
/// </summary>
public class ProjectEnvironmentManagerViewModel
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public List<EnvironmentListViewModel> Environments { get; set; } = new();
    public bool CanAddEnvironment { get; set; }
    public bool CanReorderEnvironments { get; set; }
}