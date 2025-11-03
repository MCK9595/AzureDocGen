namespace AzureDocGen.Web.Services;

/// <summary>
/// 通知サービスのインターフェース
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// 通知を作成
    /// </summary>
    Task<Notification> CreateNotificationAsync(
        string userId,
        string title,
        string message,
        NotificationType type = NotificationType.Info,
        string? actionUrl = null);

    /// <summary>
    /// ユーザーの通知一覧を取得
    /// </summary>
    Task<List<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false, int limit = 50);

    /// <summary>
    /// 通知を既読にする
    /// </summary>
    Task<bool> MarkAsReadAsync(Guid notificationId, string userId);

    /// <summary>
    /// すべての通知を既読にする
    /// </summary>
    Task<int> MarkAllAsReadAsync(string userId);

    /// <summary>
    /// 通知を削除
    /// </summary>
    Task<bool> DeleteNotificationAsync(Guid notificationId, string userId);

    /// <summary>
    /// 未読通知数を取得
    /// </summary>
    Task<int> GetUnreadCountAsync(string userId);

    /// <summary>
    /// レビュー割り当て通知を送信
    /// </summary>
    Task SendReviewAssignmentNotificationAsync(string userId, Guid workflowId, string workflowTitle);

    /// <summary>
    /// レビュー承認通知を送信
    /// </summary>
    Task SendReviewApprovedNotificationAsync(string userId, Guid workflowId, string workflowTitle);

    /// <summary>
    /// レビュー却下通知を送信
    /// </summary>
    Task SendReviewRejectedNotificationAsync(string userId, Guid workflowId, string workflowTitle, string reason);
}

/// <summary>
/// 通知エンティティ
/// </summary>
public class Notification
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? ActionUrl { get; set; }
}

/// <summary>
/// 通知タイプ
/// </summary>
public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error,
    ReviewAssignment,
    ReviewApproved,
    ReviewRejected
}
