using AzureDocGen.Data.Enums;

namespace AzureDocGen.Data.Entities;

/// <summary>
/// 環境レベルのユーザー権限
/// </summary>
public class EnvironmentUserRole
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid EnvironmentId { get; set; }
    public EnvironmentRoleType RoleType { get; set; }
    public DateTime GrantedAt { get; set; }
    public string GrantedBy { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual ApplicationUser? User { get; set; }
    public virtual Environment? Environment { get; set; }
    public virtual ApplicationUser? GrantedByUser { get; set; }
}