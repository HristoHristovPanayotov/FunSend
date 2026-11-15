namespace FunSend.Data.Entities;

/// <summary>
/// One playback, recorded for the administrator statistics.
/// <see cref="UserId"/> is null when a guest watched the video.
/// </summary>
public class VideoView
{
    public long Id { get; set; }

    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

    public int VideoId { get; set; }

    public Video? Video { get; set; }

    public string? UserId { get; set; }

    public ApplicationUser? User { get; set; }
}
