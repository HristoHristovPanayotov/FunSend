using FunSend.Data.Enums;

namespace FunSend.Services.Videos;

/// <summary>One card in a feed, a profile or a search result.</summary>
public sealed record VideoListItem
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string StoredFileName { get; init; } = string.Empty;
    public int DurationSeconds { get; init; }
    public DateTime CreatedAt { get; init; }
    public VideoVisibility Visibility { get; init; }
    public VideoStatus Status { get; init; }
    public string AuthorId { get; init; } = string.Empty;
    public string AuthorDisplayName { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public int LikeCount { get; init; }
    public int CommentCount { get; init; }
}

/// <summary>Everything the details page shows about one video.</summary>
public sealed record VideoDetails
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string StoredFileName { get; init; } = string.Empty;
    public int DurationSeconds { get; init; }
    public long FileSizeBytes { get; init; }
    public DateTime CreatedAt { get; init; }
    public VideoVisibility Visibility { get; init; }
    public VideoStatus Status { get; init; }
    public string AuthorId { get; init; } = string.Empty;
    public string AuthorDisplayName { get; init; } = string.Empty;
    public string? AuthorAvatarFileName { get; init; }
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public IReadOnlyList<string> Hashtags { get; init; } = [];
    public int LikeCount { get; init; }
    public int ViewCount { get; init; }
    public bool LikedByViewer { get; init; }
    public bool SavedByViewer { get; init; }
    public bool ViewerFollowsAuthor { get; init; }
    public bool ViewerCanDelete { get; init; }
}

/// <summary>A new upload, as the service layer wants it.</summary>
public sealed record VideoUploadRequest
{
    public required string UserId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required int CategoryId { get; init; }
    public VideoVisibility Visibility { get; init; } = VideoVisibility.Public;

    /// <summary>Original file name; only the extension is kept.</summary>
    public required string FileName { get; init; }

    public string? ContentType { get; init; }
    public required long SizeBytes { get; init; }
    public required int DurationSeconds { get; init; }

    /// <summary>The file itself, positioned at the start.</summary>
    public required Stream Content { get; init; }

    /// <summary>Tags as typed, for example "funny, cats #skate".</summary>
    public string? HashtagsRaw { get; init; }
}

/// <summary>How a feed or a search result is paged.</summary>
public sealed record PageRequest(int Page = 1, int PageSize = 12)
{
    public const int MaxPageSize = 50;

    public int Skip => (Math.Max(Page, 1) - 1) * Take;

    public int Take => Math.Clamp(PageSize, 1, MaxPageSize);
}

/// <summary>One page of results together with the total, for the pager.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;

    public static PagedResult<T> Empty(PageRequest page) => new([], 0, page.Page, page.Take);
}
