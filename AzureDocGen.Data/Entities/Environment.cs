namespace AzureDocGen.Data.Entities;

public class Environment
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    
    public virtual Project? Project { get; set; }
    public virtual ICollection<Resource> Resources { get; set; } = new List<Resource>();
}