using Microsoft.EntityFrameworkCore;
using AzureDocGen.Data.Contexts;
using AzureDocGen.Data.Entities;
using AzureDocGen.Data.Enums;

namespace AzureDocGen.Web.Services;

/// <summary>
/// レビューワークフローサービスの実装
/// </summary>
public class ReviewWorkflowService : IReviewWorkflowService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReviewWorkflowService> _logger;

    public ReviewWorkflowService(ApplicationDbContext context, ILogger<ReviewWorkflowService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ReviewWorkflow> CreateWorkflowAsync(ReviewTargetType targetType, Guid targetId, 
        Guid projectId, string title, string description, string createdBy)
    {
        var workflow = new ReviewWorkflow
        {
            Id = Guid.NewGuid(),
            TargetType = targetType,
            TargetId = targetId,
            ProjectId = projectId,
            Title = title,
            Description = description,
            Status = ReviewWorkflowStatus.Draft,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Version = 1
        };

        _context.ReviewWorkflows.Add(workflow);
        await _context.SaveChangesAsync();

        // 履歴を記録
        await AddWorkflowHistoryAsync(workflow.Id, ReviewWorkflowStatus.Draft, ReviewWorkflowStatus.Draft, 
            "Workflow created", createdBy, null);

        _logger.LogInformation("Review workflow {WorkflowId} created for {TargetType} {TargetId} by {CreatedBy}",
            workflow.Id, targetType, targetId, createdBy);

        return workflow;
    }

    public async Task AssignReviewersAsync(Guid workflowId, List<string> reviewerIds, string assignedBy)
    {
        var workflow = await _context.ReviewWorkflows
            .FirstOrDefaultAsync(w => w.Id == workflowId);

        if (workflow == null)
            throw new ArgumentException("Workflow not found", nameof(workflowId));

        if (workflow.Status != ReviewWorkflowStatus.Draft)
            throw new InvalidOperationException("Can only assign reviewers to draft workflows");

        // 既存の割り当てを削除
        var existingAssignments = await _context.ReviewAssignments
            .Where(ra => ra.WorkflowId == workflowId)
            .ToListAsync();
        _context.ReviewAssignments.RemoveRange(existingAssignments);

        // 新しい割り当てを作成
        foreach (var reviewerId in reviewerIds)
        {
            var assignment = new ReviewAssignment
            {
                Id = Guid.NewGuid(),
                WorkflowId = workflowId,
                ReviewerId = reviewerId,
                Status = ReviewAssignmentStatus.Pending,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = assignedBy
            };
            _context.ReviewAssignments.Add(assignment);
        }

        // ワークフロー状態をレビュー中に変更
        workflow.Status = ReviewWorkflowStatus.InReview;
        workflow.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // 履歴を記録
        await AddWorkflowHistoryAsync(workflowId, ReviewWorkflowStatus.Draft, ReviewWorkflowStatus.InReview,
            $"Reviewers assigned: {string.Join(", ", reviewerIds)}", assignedBy, null);

        // 通知を送信
        await SendReviewNotificationAsync(workflowId, reviewerIds, "ReviewAssigned");

        _logger.LogInformation("Reviewers assigned to workflow {WorkflowId}: {ReviewerIds} by {AssignedBy}",
            workflowId, string.Join(", ", reviewerIds), assignedBy);
    }

    public async Task<bool> ApproveReviewAsync(Guid workflowId, string reviewerId, string? comment = null)
    {
        var assignment = await _context.ReviewAssignments
            .Include(ra => ra.Workflow)
            .FirstOrDefaultAsync(ra => ra.WorkflowId == workflowId && ra.ReviewerId == reviewerId);

        if (assignment == null || assignment.Status != ReviewAssignmentStatus.Pending)
            return false;

        // レビュー承認を記録
        assignment.Status = ReviewAssignmentStatus.Approved;
        assignment.Comment = comment;
        assignment.ReviewedAt = DateTime.UtcNow;

        // 他の待機中レビューをスキップに変更（1名承認で完了）
        var otherPendingAssignments = await _context.ReviewAssignments
            .Where(ra => ra.WorkflowId == workflowId && ra.ReviewerId != reviewerId && 
                        ra.Status == ReviewAssignmentStatus.Pending)
            .ToListAsync();

        foreach (var otherAssignment in otherPendingAssignments)
        {
            otherAssignment.Status = ReviewAssignmentStatus.Skipped;
        }

        // ワークフロー状態を承認済みに変更
        var workflow = assignment.Workflow!;
        workflow.Status = ReviewWorkflowStatus.Approved;
        workflow.UpdatedAt = DateTime.UtcNow;
        workflow.ApprovedAt = DateTime.UtcNow;
        workflow.ApprovedBy = reviewerId;

        await _context.SaveChangesAsync();

        // 履歴を記録
        await AddWorkflowHistoryAsync(workflowId, ReviewWorkflowStatus.InReview, ReviewWorkflowStatus.Approved,
            "Review approved", reviewerId, comment);

        _logger.LogInformation("Workflow {WorkflowId} approved by reviewer {ReviewerId}", workflowId, reviewerId);

        return true;
    }

    public async Task<bool> RejectReviewAsync(Guid workflowId, string reviewerId, string comment)
    {
        var assignment = await _context.ReviewAssignments
            .Include(ra => ra.Workflow)
            .FirstOrDefaultAsync(ra => ra.WorkflowId == workflowId && ra.ReviewerId == reviewerId);

        if (assignment == null || assignment.Status != ReviewAssignmentStatus.Pending)
            return false;

        // レビュー差し戻しを記録
        assignment.Status = ReviewAssignmentStatus.Rejected;
        assignment.Comment = comment;
        assignment.ReviewedAt = DateTime.UtcNow;

        // 他の待機中レビューもスキップに変更
        var otherPendingAssignments = await _context.ReviewAssignments
            .Where(ra => ra.WorkflowId == workflowId && ra.ReviewerId != reviewerId && 
                        ra.Status == ReviewAssignmentStatus.Pending)
            .ToListAsync();

        foreach (var otherAssignment in otherPendingAssignments)
        {
            otherAssignment.Status = ReviewAssignmentStatus.Skipped;
        }

        // ワークフロー状態を差し戻しに変更
        var workflow = assignment.Workflow!;
        workflow.Status = ReviewWorkflowStatus.Rejected;
        workflow.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // 履歴を記録
        await AddWorkflowHistoryAsync(workflowId, ReviewWorkflowStatus.InReview, ReviewWorkflowStatus.Rejected,
            "Review rejected", reviewerId, comment);

        _logger.LogInformation("Workflow {WorkflowId} rejected by reviewer {ReviewerId}", workflowId, reviewerId);

        return true;
    }

    public async Task<bool> CancelWorkflowAsync(Guid workflowId, string cancelledBy, string? reason = null)
    {
        var workflow = await _context.ReviewWorkflows
            .FirstOrDefaultAsync(w => w.Id == workflowId);

        if (workflow == null || workflow.Status == ReviewWorkflowStatus.Cancelled)
            return false;

        var previousStatus = workflow.Status;
        workflow.Status = ReviewWorkflowStatus.Cancelled;
        workflow.UpdatedAt = DateTime.UtcNow;

        // 待機中の割り当てをスキップに変更
        var pendingAssignments = await _context.ReviewAssignments
            .Where(ra => ra.WorkflowId == workflowId && ra.Status == ReviewAssignmentStatus.Pending)
            .ToListAsync();

        foreach (var assignment in pendingAssignments)
        {
            assignment.Status = ReviewAssignmentStatus.Skipped;
        }

        await _context.SaveChangesAsync();

        // 履歴を記録
        await AddWorkflowHistoryAsync(workflowId, previousStatus, ReviewWorkflowStatus.Cancelled,
            "Workflow cancelled", cancelledBy, reason);

        _logger.LogInformation("Workflow {WorkflowId} cancelled by {CancelledBy}", workflowId, cancelledBy);

        return true;
    }

    public async Task<ReviewWorkflow?> GetWorkflowAsync(Guid workflowId)
    {
        return await _context.ReviewWorkflows
            .Include(w => w.ReviewAssignments)
                .ThenInclude(ra => ra.Reviewer)
            .Include(w => w.WorkflowHistories)
            .Include(w => w.Creator)
            .Include(w => w.Approver)
            .FirstOrDefaultAsync(w => w.Id == workflowId);
    }

    public async Task<List<ReviewWorkflow>> GetProjectWorkflowsAsync(Guid projectId, ReviewWorkflowStatus? status = null)
    {
        var query = _context.ReviewWorkflows
            .Include(w => w.ReviewAssignments)
                .ThenInclude(ra => ra.Reviewer)
            .Include(w => w.Creator)
            .Where(w => w.ProjectId == projectId);

        if (status.HasValue)
        {
            query = query.Where(w => w.Status == status.Value);
        }

        return await query
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ReviewAssignment>> GetUserReviewAssignmentsAsync(string userId, ReviewAssignmentStatus? status = null)
    {
        var query = _context.ReviewAssignments
            .Include(ra => ra.Workflow)
                .ThenInclude(w => w!.Project)
            .Include(ra => ra.Workflow)
                .ThenInclude(w => w!.Creator)
            .Where(ra => ra.ReviewerId == userId);

        if (status.HasValue)
        {
            query = query.Where(ra => ra.Status == status.Value);
        }

        return await query
            .OrderByDescending(ra => ra.AssignedAt)
            .ToListAsync();
    }

    public async Task<List<WorkflowHistory>> GetWorkflowHistoryAsync(Guid workflowId)
    {
        return await _context.WorkflowHistories
            .Include(wh => wh.Actor)
            .Where(wh => wh.WorkflowId == workflowId)
            .OrderByDescending(wh => wh.ActionAt)
            .ToListAsync();
    }

    public async Task<bool> IsWorkflowCompleteAsync(Guid workflowId)
    {
        var workflow = await _context.ReviewWorkflows
            .FirstOrDefaultAsync(w => w.Id == workflowId);

        return workflow?.Status == ReviewWorkflowStatus.Approved ||
               workflow?.Status == ReviewWorkflowStatus.Rejected ||
               workflow?.Status == ReviewWorkflowStatus.Cancelled;
    }

    public async Task SendReviewNotificationAsync(Guid workflowId, List<string> reviewerIds, string action)
    {
        // TODO: 実際の通知実装（メール、アプリ内通知など）
        _logger.LogInformation("Review notification sent for workflow {WorkflowId}, action: {Action}, reviewers: {ReviewerIds}",
            workflowId, action, string.Join(", ", reviewerIds));
        
        await Task.CompletedTask;
    }

    private async Task AddWorkflowHistoryAsync(Guid workflowId, ReviewWorkflowStatus fromStatus, 
        ReviewWorkflowStatus toStatus, string action, string actorId, string? comment)
    {
        var history = new WorkflowHistory
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflowId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Action = action,
            Comment = comment,
            ActorId = actorId,
            ActionAt = DateTime.UtcNow
        };

        _context.WorkflowHistories.Add(history);
        await _context.SaveChangesAsync();
    }
}