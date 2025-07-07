using AzureDocGen.Data.Enums;

namespace AzureDocGen.Data.Entities;

/// <summary>
/// レビューワークフロー
/// </summary>
public class ReviewWorkflow
{
    public Guid Id { get; set; }
    public ReviewTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ReviewWorkflowStatus Status { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public int Version { get; set; } = 1;
    
    // Navigation properties
    public virtual Project? Project { get; set; }
    public virtual ApplicationUser? Creator { get; set; }
    public virtual ApplicationUser? Approver { get; set; }
    public virtual ICollection<ReviewAssignment> ReviewAssignments { get; set; } = new List<ReviewAssignment>();
    public virtual ICollection<WorkflowHistory> WorkflowHistories { get; set; } = new List<WorkflowHistory>();
}