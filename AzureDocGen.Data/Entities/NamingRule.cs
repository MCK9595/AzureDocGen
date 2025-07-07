namespace AzureDocGen.Data.Entities;

public class NamingRule
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public string Example { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    
    public virtual Project? Project { get; set; }
}