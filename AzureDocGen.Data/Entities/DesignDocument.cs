namespace AzureDocGen.Data.Entities;

public class DesignDocument
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public WorkflowStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    
    public virtual Project? Project { get; set; }
    public virtual ApplicationUser? Creator { get; set; }
    public virtual ApplicationUser? Approver { get; set; }
    public virtual ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
}

public enum WorkflowStatus
{
    Draft = 0,
    InReview = 1,
    Approved = 2,
    Rejected = 3,
    Archived = 4
}

public class DocumentVersion
{
    public Guid Id { get; set; }
    public Guid DesignDocumentId { get; set; }
    public int Version { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    
    public virtual DesignDocument? DesignDocument { get; set; }
    public virtual ApplicationUser? Creator { get; set; }
}