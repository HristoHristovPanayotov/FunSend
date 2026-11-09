namespace FunSend.Data.Entities;

/// <summary>A video a user put aside for later; composite key (VideoId, UserId).</summary>
public class SavedVideo
{
    public int VideoId { get; set; }

    public Video? Video { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
