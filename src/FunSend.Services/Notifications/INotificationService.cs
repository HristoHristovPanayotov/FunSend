using FunSend.Data.Enums;
using FunSend.Services.Common;
using FunSend.Services.Videos;

namespace FunSend.Services.Notifications;

/// <summary>One line in the notification list.</summary>
public sealed record NotificationItem
{
    public int Id { get; init; }
    public NotificationType Type { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? ActorId { get; init; }
    public string? ActorDisplayName { get; init; }
    public int? VideoId { get; init; }
    public string? VideoTitle { get; init; }
}

/// <summary>
/// Creates and reads the notifications a user sees. Nothing is ever sent to a
/// user about their own action, which is checked here rather than at each call
/// site.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Adds a notification for <paramref name="recipientId"/>. Returns without
    /// writing when the recipient is also the actor.
    /// </summary>
    Task<ServiceResult> NotifyAsync(
        string recipientId,
        NotificationType type,
        string message,
        string? actorId = null,
        int? videoId = null,
        CancellationToken cancellationToken = default);

    Task<PagedResult<NotificationItem>> GetForUserAsync(
        string userId,
        PageRequest page,
        bool unreadOnly = false,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Marks one notification read; Forbidden when it belongs to somebody else.</summary>
    Task<ServiceResult> MarkAsReadAsync(
        int notificationId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);
}
