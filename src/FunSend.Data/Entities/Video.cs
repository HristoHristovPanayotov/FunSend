using FunSend.Data.Enums;

namespace FunSend.Data.Entities;

/// <summary>
/// An uploaded video. Only metadata lives in the database; the file itself is
/// kept by the storage layer and referenced through <see cref="StoredFileName"/>.
/// </summary>
public class Video
{
    public const int TitleMaxLength = 120;
    public const int DescriptionMaxLength = 1000;
    public const int StoredFileNameMaxLength = 80;

    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Generated (GUID based) name of the file on disk.</summary>
    public string StoredFileName { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public int DurationSeconds { get; set; }

    public VideoStatus Status { get; set; } = VideoStatus.Published;

    public VideoVisibility Visibility { get; set; } = VideoVisibility.Public;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public int CategoryId { get; set; }

    public Category? Category { get; set; }

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public ICollection<SavedVideo> SavedBy { get; set; } = new List<SavedVideo>();
    public ICollection<VideoHashtag> VideoHashtags { get; set; } = new List<VideoHashtag>();
    public ICollection<VideoView> Views { get; set; } = new List<VideoView>();
}
