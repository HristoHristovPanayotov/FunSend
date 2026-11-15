using FunSend.Data;
using FunSend.Data.Entities;
using FunSend.Data.Enums;
using FunSend.Services.Common;
using FunSend.Services.Videos;
using Microsoft.EntityFrameworkCore;

namespace FunSend.Services.Notifications;

/// <inheritdoc cref="INotificationService"/>
public class NotificationService : INotificationService
{
    private readonly FunSendDbContext _db;

    public NotificationService(FunSendDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult> NotifyAsync(
        string recipientId,
        NotificationType type,
        string message,
        string? actorId = null,
        int? videoId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(recipientId))
        {
            return ServiceResult.Invalid("A notification needs a recipient.");
        }

        // Nobody is told about their own action.
        if (actorId is not null && actorId == recipientId)
        {
            return ServiceResult.Success();
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return ServiceResult.Invalid("A notification needs a message.");
        }

        var recipientExists = await _db.Users
            .AnyAsync(u => u.Id == recipientId, cancellationToken);

        if (!recipientExists)
        {
            return ServiceResult.NotFound("The recipient does not exist.");
        }

        _db.Notifications.Add(new Notification
        {
            UserId = recipientId,
            ActorId = actorId,
            VideoId = videoId,
            Type = type,
            Message = Shorten(message),
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<PagedResult<NotificationItem>> GetForUserAsync(
        string userId,
        PageRequest page,
        bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return PagedResult<NotificationItem>.Empty(page);
        }

        var query = _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Skip(page.Skip)
            .Take(page.Take)
            .Select(n => new NotificationItem
            {
                Id = n.Id,
                Type = n.Type,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                ActorId = n.ActorId,
                ActorDisplayName = n.Actor != null ? n.Actor.DisplayName : null,
                VideoId = n.VideoId,
                VideoTitle = n.Video != null ? n.Video.Title : null
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<NotificationItem>(items, total, Math.Max(page.Page, 1), page.Take);
    }

    public Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default)
        => string.IsNullOrEmpty(userId)
            ? Task.FromResult(0)
            : _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

    public async Task<ServiceResult> MarkAsReadAsync(
        int notificationId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification is null)
        {
            return ServiceResult.NotFound("The notification does not exist.");
        }

        if (notification.UserId != userId)
        {
            return ServiceResult.Forbidden("The notification belongs to another user.");
        }

        if (notification.IsRead)
        {
            return ServiceResult.Success();
        }

        notification.IsRead = true;
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> MarkAllAsReadAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        if (unread.Count == 0)
        {
            return ServiceResult.Success();
        }

        foreach (var notification in unread)
        {
            notification.IsRead = true;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    private static string Shorten(string message)
    {
        var trimmed = message.Trim();

        return trimmed.Length > Notification.MessageMaxLength
            ? trimmed[..Notification.MessageMaxLength]
            : trimmed;
    }
}
