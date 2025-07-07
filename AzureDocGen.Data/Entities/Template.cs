using System.Text.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace AzureDocGen.Data.Entities;

public class Template
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? StructureJson { get; set; }
    public int Version { get; set; }
    public SharingLevel SharingLevel { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    
    public virtual ICollection<TemplateParameter> Parameters { get; set; } = new List<TemplateParameter>();
    public virtual ApplicationUser? Creator { get; set; }

    // Helper property for JSON serialization/deserialization
    [NotMapped]
    public Dictionary<string, object>? Structure
    {
        get => string.IsNullOrEmpty(StructureJson) 
            ? null 
            : JsonSerializer.Deserialize<Dictionary<string, object>>(StructureJson);
        set => StructureJson = value == null 
            ? null 
            : JsonSerializer.Serialize(value);
    }
}

public enum SharingLevel
{
    Private = 0,
    Project = 1,
    Global = 2
}