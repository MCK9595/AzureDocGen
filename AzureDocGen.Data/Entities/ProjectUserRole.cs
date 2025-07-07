using AzureDocGen.Data.Enums;

namespace AzureDocGen.Data.Entities;

/// <summary>
/// プロジェクトレベルのユーザー権限
/// </summary>
public class ProjectUserRole
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public ProjectRoleType RoleType { get; set; }
    public DateTime GrantedAt { get; set; }
    public string GrantedBy { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual ApplicationUser? User { get; set; }
    public virtual Project? Project { get; set; }
    public virtual ApplicationUser? GrantedByUser { get; set; }
}