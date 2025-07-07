using AzureDocGen.Data.Enums;

namespace AzureDocGen.Data.Entities;

/// <summary>
/// レビュー割り当て
/// </summary>
public class ReviewAssignment
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public string ReviewerId { get; set; } = string.Empty;
    public ReviewAssignmentStatus Status { get; set; }
    public string? Comment { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string AssignedBy { get; set; } = string.Empty;
    
    // Navigation properties
    public virtual ReviewWorkflow? Workflow { get; set; }
    public virtual ApplicationUser? Reviewer { get; set; }
    public virtual ApplicationUser? AssignedByUser { get; set; }
}