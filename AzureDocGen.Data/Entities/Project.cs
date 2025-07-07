namespace AzureDocGen.Data.Entities;

public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    
    public virtual ICollection<Environment> Environments { get; set; } = new List<Environment>();
    public virtual ICollection<NamingRule> NamingRules { get; set; } = new List<NamingRule>();
    public virtual ICollection<ProjectUserRole> ProjectUserRoles { get; set; } = new List<ProjectUserRole>();
    public virtual ApplicationUser? Creator { get; set; }
}