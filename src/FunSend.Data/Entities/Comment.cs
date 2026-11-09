namespace FunSend.Data.Entities;

/// <summary>A comment under a video. Moderation hides it instead of deleting it.</summary>
public class Comment
{
    public const int TextMaxLength = 500;

    public int Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsRemoved { get; set; }

    public int VideoId { get; set; }

    public Video? Video { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }
}
