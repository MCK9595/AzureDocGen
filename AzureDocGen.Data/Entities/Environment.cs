namespace AzureDocGen.Data.Entities;

public class Environment
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    
    public virtual Project? Project { get; set; }
    public virtual ApplicationUser? Creator { get; set; }
    public virtual ICollection<Resource> Resources { get; set; } = new List<Resource>();
    public virtual ICollection<DesignDocument> DesignDocuments { get; set; } = new List<DesignDocument>();
}