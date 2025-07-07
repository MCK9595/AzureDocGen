using AzureDocGen.Data.Enums;

namespace AzureDocGen.Data.Entities;

/// <summary>
/// システムレベルの権限
/// </summary>
public class SystemRole
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public SystemRoleType RoleType { get; set; }
    public DateTime GrantedAt { get; set; }
    public string GrantedBy { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual ApplicationUser? User { get; set; }
    public virtual ApplicationUser? GrantedByUser { get; set; }
}