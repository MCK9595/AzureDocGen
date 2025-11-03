using System.ComponentModel.DataAnnotations;
using AzureDocGen.Data.Entities;

namespace AzureDocGen.Web.Models;

/// <summary>
/// 設計書一覧ビューモデル
/// </summary>
public class DesignDocumentListViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public WorkflowStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public int VersionCount { get; set; }
}

/// <summary>
/// 設計書詳細ビューモデル
/// </summary>
public class DesignDocumentDetailsViewModel
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public WorkflowStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public List<DocumentVersionViewModel> Versions { get; set; } = new();
    public bool CanEdit { get; set; }
    public bool CanApprove { get; set; }
    public bool CanDelete { get; set; }
}

/// <summary>
/// ドキュメントバージョンビューモデル
/// </summary>
public class DocumentVersionViewModel
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
}

/// <summary>
/// 設計書作成ビューモデル
/// </summary>
public class DesignDocumentCreateViewModel
{
    public Guid ProjectId { get; set; }

    [Required(ErrorMessage = "タイトルは必須です。")]
    [StringLength(200, ErrorMessage = "タイトルは{1}文字以内で入力してください。")]
    [Display(Name = "タイトル")]
    public string Title { get; set; } = string.Empty;
}

/// <summary>
/// 設計書編集ビューモデル
/// </summary>
public class DesignDocumentEditViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "タイトルは必須です。")]
    [StringLength(200, ErrorMessage = "タイトルは{1}文字以内で入力してください。")]
    [Display(Name = "タイトル")]
    public string Title { get; set; } = string.Empty;
}

/// <summary>
/// リソースビューモデル
/// </summary>
public class ResourceViewModel
{
    public Guid Id { get; set; }
    public Guid EnvironmentId { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

    // Position property for designer compatibility
    public Position Position
    {
        get => VisualPosition ?? new Position { X = 100, Y = 100, Width = 120, Height = 80 };
        set => VisualPosition = value;
    }

    public Position? VisualPosition { get; set; }
}

/// <summary>
/// リソース作成ビューモデル
/// </summary>
public class ResourceCreateViewModel
{
    public Guid EnvironmentId { get; set; }

    [Required(ErrorMessage = "リソースタイプは必須です。")]
    [Display(Name = "リソースタイプ")]
    public string ResourceType { get; set; } = string.Empty;

    [Required(ErrorMessage = "リソース名は必須です。")]
    [StringLength(100, ErrorMessage = "リソース名は{1}文字以内で入力してください。")]
    [Display(Name = "リソース名")]
    public string Name { get; set; } = string.Empty;

    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 120;
    public double Height { get; set; } = 80;
}

/// <summary>
/// リソース更新ビューモデル
/// </summary>
public class ResourceUpdateViewModel
{
    public Guid Id { get; set; }

    [StringLength(100, ErrorMessage = "リソース名は{1}文字以内で入力してください。")]
    [Display(Name = "リソース名")]
    public string? Name { get; set; }

    public Dictionary<string, object>? Properties { get; set; }
}

/// <summary>
/// リソース位置更新ビューモデル
/// </summary>
public class ResourcePositionUpdateViewModel
{
    public Guid ResourceId { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
}

/// <summary>
/// リソース接続ビューモデル
/// </summary>
public class ResourceConnectionViewModel
{
    public Guid Id { get; set; }
    public Guid SourceResourceId { get; set; }
    public Guid TargetResourceId { get; set; }
    public string ConnectionType { get; set; } = string.Empty;
    public string SourceResourceName { get; set; } = string.Empty;
    public string TargetResourceName { get; set; } = string.Empty;
}

/// <summary>
/// リソース接続作成ビューモデル
/// </summary>
public class ResourceConnectionCreateViewModel
{
    [Required(ErrorMessage = "接続元リソースは必須です。")]
    public Guid SourceResourceId { get; set; }

    [Required(ErrorMessage = "接続先リソースは必須です。")]
    public Guid TargetResourceId { get; set; }

    [Required(ErrorMessage = "接続タイプは必須です。")]
    [Display(Name = "接続タイプ")]
    public string ConnectionType { get; set; } = "default";
}

/// <summary>
/// ビジュアルデザイナービューモデル
/// </summary>
public class VisualDesignerViewModel
{
    public Guid EnvironmentId { get; set; }
    public string EnvironmentName { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public List<ResourceViewModel> Resources { get; set; } = new();
    public List<ResourceConnectionViewModel> Connections { get; set; } = new();
    public bool CanEdit { get; set; }
}

/// <summary>
/// Azure リソースタイプ定義
/// </summary>
public static class AzureResourceTypes
{
    public static readonly List<ResourceTypeInfo> AllTypes = new()
    {
        new ResourceTypeInfo { Type = "VirtualMachine", DisplayName = "仮想マシン", Category = "コンピューティング", Icon = "💻" },
        new ResourceTypeInfo { Type = "AppService", DisplayName = "App Service", Category = "コンピューティング", Icon = "🌐" },
        new ResourceTypeInfo { Type = "FunctionApp", DisplayName = "Function App", Category = "コンピューティング", Icon = "⚡" },
        new ResourceTypeInfo { Type = "ContainerInstance", DisplayName = "Container Instances", Category = "コンピューティング", Icon = "📦" },
        new ResourceTypeInfo { Type = "KubernetesService", DisplayName = "Kubernetes Service", Category = "コンピューティング", Icon = "☸️" },

        new ResourceTypeInfo { Type = "StorageAccount", DisplayName = "Storage Account", Category = "ストレージ", Icon = "💾" },
        new ResourceTypeInfo { Type = "BlobStorage", DisplayName = "Blob Storage", Category = "ストレージ", Icon = "📦" },
        new ResourceTypeInfo { Type = "FileStorage", DisplayName = "File Storage", Category = "ストレージ", Icon = "📁" },

        new ResourceTypeInfo { Type = "SqlDatabase", DisplayName = "SQL Database", Category = "データベース", Icon = "🗄️" },
        new ResourceTypeInfo { Type = "CosmosDb", DisplayName = "Cosmos DB", Category = "データベース", Icon = "🌍" },
        new ResourceTypeInfo { Type = "MySql", DisplayName = "MySQL", Category = "データベース", Icon = "🐬" },
        new ResourceTypeInfo { Type = "PostgreSql", DisplayName = "PostgreSQL", Category = "データベース", Icon = "🐘" },

        new ResourceTypeInfo { Type = "VirtualNetwork", DisplayName = "Virtual Network", Category = "ネットワーク", Icon = "🌐" },
        new ResourceTypeInfo { Type = "LoadBalancer", DisplayName = "Load Balancer", Category = "ネットワーク", Icon = "⚖️" },
        new ResourceTypeInfo { Type = "ApplicationGateway", DisplayName = "Application Gateway", Category = "ネットワーク", Icon = "🚪" },
        new ResourceTypeInfo { Type = "VpnGateway", DisplayName = "VPN Gateway", Category = "ネットワーク", Icon = "🔒" },
        new ResourceTypeInfo { Type = "Firewall", DisplayName = "Firewall", Category = "ネットワーク", Icon = "🛡️" },

        new ResourceTypeInfo { Type = "KeyVault", DisplayName = "Key Vault", Category = "セキュリティ", Icon = "🔐" },
        new ResourceTypeInfo { Type = "SecurityCenter", DisplayName = "Security Center", Category = "セキュリティ", Icon = "🛡️" },

        new ResourceTypeInfo { Type = "Monitor", DisplayName = "Monitor", Category = "管理", Icon = "📊" },
        new ResourceTypeInfo { Type = "LogAnalytics", DisplayName = "Log Analytics", Category = "管理", Icon = "📈" }
    };

    public static List<string> GetCategories()
    {
        return AllTypes.Select(t => t.Category).Distinct().OrderBy(c => c).ToList();
    }

    public static List<ResourceTypeInfo> GetTypesByCategory(string category)
    {
        return AllTypes.Where(t => t.Category == category).ToList();
    }
}

public class ResourceTypeInfo
{
    public string Type { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
