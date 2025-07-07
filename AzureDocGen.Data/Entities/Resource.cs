using System.Text.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace AzureDocGen.Data.Entities;

public class Resource
{
    public Guid Id { get; set; }
    public Guid EnvironmentId { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? PropertiesJson { get; set; }
    public Position? VisualPosition { get; set; }
    
    public virtual Environment? Environment { get; set; }
    public virtual ICollection<ResourceConnection> Connections { get; set; } = new List<ResourceConnection>();

    // Helper properties for JSON serialization/deserialization
    [NotMapped]
    public Dictionary<string, object>? Properties
    {
        get => string.IsNullOrEmpty(PropertiesJson) 
            ? null 
            : JsonSerializer.Deserialize<Dictionary<string, object>>(PropertiesJson);
        set => PropertiesJson = value == null 
            ? null 
            : JsonSerializer.Serialize(value);
    }
}

public class Position
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

public class ResourceConnection
{
    public Guid Id { get; set; }
    public Guid SourceResourceId { get; set; }
    public Guid TargetResourceId { get; set; }
    public string ConnectionType { get; set; } = string.Empty;
    
    public virtual Resource? SourceResource { get; set; }
    public virtual Resource? TargetResource { get; set; }
}