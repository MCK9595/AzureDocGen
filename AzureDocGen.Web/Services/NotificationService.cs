using System.Collections.Concurrent;

namespace AzureDocGen.Web.Services;

/// <summary>
/// 通知サービスの実装（インメモリ）
/// </summary>
public class NotificationService : INotificationService
{
    private static readonly ConcurrentDictionary<Guid, Notification> _notifications = new();
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public Task<Notification> CreateNotificationAsync(
        string userId,
        string title,
        string message,
        NotificationType type = NotificationType.Info,
        string? actionUrl = null)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            ActionUrl = actionUrl
        };

        _notifications.TryAdd(notification.Id, notification);

        _logger.LogInformation("Notification created for user {UserId}: {Title}", userId, title);

        return Task.FromResult(notification);
    }

    public Task<List<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false, int limit = 50)
    {
        var notifications = _notifications.Values
            .Where(n => n.UserId == userId)
            .Where(n => !unreadOnly || !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToList();

        return Task.FromResult(notifications);
    }

    public Task<bool> MarkAsReadAsync(Guid notificationId, string userId)
    {
        if (_notifications.TryGetValue(notificationId, out var notification))
        {
            if (notification.UserId == userId)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    public Task<int> MarkAllAsReadAsync(string userId)
    {
        var count = 0;
        var userNotifications = _notifications.Values
            .Where(n => n.UserId == userId && !n.IsRead);

        foreach (var notification in userNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            count++;
        }

        _logger.LogInformation("Marked {Count} notifications as read for user {UserId}", count, userId);

        return Task.FromResult(count);
    }

    public Task<bool> DeleteNotificationAsync(Guid notificationId, string userId)
    {
        if (_notifications.TryGetValue(notificationId, out var notification))
        {
            if (notification.UserId == userId)
            {
                _notifications.TryRemove(notificationId, out _);
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    public Task<int> GetUnreadCountAsync(string userId)
    {
        var count = _notifications.Values
            .Count(n => n.UserId == userId && !n.IsRead);

        return Task.FromResult(count);
    }

    public async Task SendReviewAssignmentNotificationAsync(string userId, Guid workflowId, string workflowTitle)
    {
        await CreateNotificationAsync(
            userId,
            "レビューが割り当てられました",
            $"ワークフロー「{workflowTitle}」のレビューが割り当てられました。",
            NotificationType.ReviewAssignment,
            $"/Workflow/Review/{workflowId}"
        );
    }

    public async Task SendReviewApprovedNotificationAsync(string userId, Guid workflowId, string workflowTitle)
    {
        await CreateNotificationAsync(
            userId,
            "レビューが承認されました",
            $"ワークフロー「{workflowTitle}」が承認されました。",
            NotificationType.ReviewApproved,
            $"/Workflow/History/{workflowId}"
        );
    }

    public async Task SendReviewRejectedNotificationAsync(string userId, Guid workflowId, string workflowTitle, string reason)
    {
        await CreateNotificationAsync(
            userId,
            "レビューが却下されました",
            $"ワークフロー「{workflowTitle}」が却下されました。\n理由: {reason}",
            NotificationType.ReviewRejected,
            $"/Workflow/History/{workflowId}"
        );
    }
}
