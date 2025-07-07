using AzureDocGen.Data.Entities;
using AzureDocGen.Data.Enums;

namespace AzureDocGen.Web.Services;

/// <summary>
/// レビューワークフローサービスのインターフェース
/// </summary>
public interface IReviewWorkflowService
{
    /// <summary>
    /// レビューワークフローを作成
    /// </summary>
    Task<ReviewWorkflow> CreateWorkflowAsync(ReviewTargetType targetType, Guid targetId, 
        Guid projectId, string title, string description, string createdBy);
    
    /// <summary>
    /// レビューアーを割り当て
    /// </summary>
    Task AssignReviewersAsync(Guid workflowId, List<string> reviewerIds, string assignedBy);
    
    /// <summary>
    /// レビューを承認
    /// </summary>
    Task<bool> ApproveReviewAsync(Guid workflowId, string reviewerId, string? comment = null);
    
    /// <summary>
    /// レビューを差し戻し
    /// </summary>
    Task<bool> RejectReviewAsync(Guid workflowId, string reviewerId, string comment);
    
    /// <summary>
    /// ワークフローをキャンセル
    /// </summary>
    Task<bool> CancelWorkflowAsync(Guid workflowId, string cancelledBy, string? reason = null);
    
    /// <summary>
    /// ワークフローの状態を取得
    /// </summary>
    Task<ReviewWorkflow?> GetWorkflowAsync(Guid workflowId);
    
    /// <summary>
    /// プロジェクトのワークフロー一覧を取得
    /// </summary>
    Task<List<ReviewWorkflow>> GetProjectWorkflowsAsync(Guid projectId, ReviewWorkflowStatus? status = null);
    
    /// <summary>
    /// ユーザーが担当するレビュー一覧を取得
    /// </summary>
    Task<List<ReviewAssignment>> GetUserReviewAssignmentsAsync(string userId, ReviewAssignmentStatus? status = null);
    
    /// <summary>
    /// ワークフローの履歴を取得
    /// </summary>
    Task<List<WorkflowHistory>> GetWorkflowHistoryAsync(Guid workflowId);
    
    /// <summary>
    /// ワークフローが完了しているかチェック
    /// </summary>
    Task<bool> IsWorkflowCompleteAsync(Guid workflowId);
    
    /// <summary>
    /// レビュー通知を送信（実装は後で追加）
    /// </summary>
    Task SendReviewNotificationAsync(Guid workflowId, List<string> reviewerIds, string action);
}