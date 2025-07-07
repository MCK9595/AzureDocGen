namespace AzureDocGen.Data.Entities;

public class TemplateParameter
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public ParameterType Type { get; set; }
    public bool IsRequired { get; set; }
    public string DefaultValue { get; set; } = string.Empty;
    public string ValidationRule { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
    
    public virtual Template? Template { get; set; }
}

public enum ParameterType
{
    Text = 0,
    Number = 1,
    Boolean = 2,
    Select = 3,
    Date = 4,
    Email = 5,
    Url = 6
}