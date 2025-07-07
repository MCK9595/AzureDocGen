using Microsoft.AspNetCore.Identity;

namespace AzureDocGen.Data.Entities;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}