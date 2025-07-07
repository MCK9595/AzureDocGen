using AzureDocGen.Data.Enums;

namespace AzureDocGen.Data.Entities;

/// <summary>
/// ワークフロー履歴
/// </summary>
public class WorkflowHistory
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public ReviewWorkflowStatus FromStatus { get; set; }
    public ReviewWorkflowStatus ToStatus { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public string ActorId { get; set; } = string.Empty;
    public DateTime ActionAt { get; set; }
    
    // Navigation properties
    public virtual ReviewWorkflow? Workflow { get; set; }
    public virtual ApplicationUser? Actor { get; set; }
}