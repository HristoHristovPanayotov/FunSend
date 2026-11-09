using FunSend.Data.Enums;

namespace FunSend.Data.Entities;

/// <summary>
/// A message for <see cref="UserId"/> about something <see cref="ActorId"/> did.
/// </summary>
public class Notification
{
    public const int MessageMaxLength = 300;

    public int Id { get; set; }

    public NotificationType Type { get; set; }

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>The user who receives the notification.</summary>
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    /// <summary>The user who caused it; null when the system did.</summary>
    public string? ActorId { get; set; }

    public ApplicationUser? Actor { get; set; }

    public int? VideoId { get; set; }

    public Video? Video { get; set; }
}
