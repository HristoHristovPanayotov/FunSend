namespace FunSend.Data.Entities;

/// <summary>
/// One like of one video by one user. The composite key (VideoId, UserId)
/// makes a second like by the same user impossible at the database level.
/// </summary>
public class Like
{
    public int VideoId { get; set; }

    public Video? Video { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
