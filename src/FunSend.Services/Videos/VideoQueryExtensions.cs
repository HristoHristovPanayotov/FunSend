using FunSend.Data;
using FunSend.Data.Entities;
using FunSend.Data.Enums;

namespace FunSend.Services.Videos;

/// <summary>
/// The visibility rule of the platform, written once as a query filter so that
/// every feed, profile and search goes through the same test. Keeping it here
/// rather than in each controller is what makes it testable on its own.
/// </summary>
public static class VideoQueryExtensions
{
    /// <summary>
    /// Narrows a video query to the rows <paramref name="viewerId"/> may see.
    ///
    /// A removed video is visible to nobody but an administrator and its own
    /// author. Beyond that: Public is visible to everyone including guests,
    /// Private only to its author, and FollowersOnly to its author and to the
    /// users who follow the author.
    /// </summary>
    /// <param name="viewerId">The signed-in user, or null for a guest.</param>
    /// <param name="isAdmin">An administrator moderates, so sees everything.</param>
    public static IQueryable<Video> VisibleTo(
        this IQueryable<Video> videos,
        FunSendDbContext db,
        string? viewerId,
        bool isAdmin = false)
    {
        ArgumentNullException.ThrowIfNull(videos);
        ArgumentNullException.ThrowIfNull(db);

        if (isAdmin)
        {
            return videos;
        }

        if (string.IsNullOrEmpty(viewerId))
        {
            return videos.Where(v =>
                v.Status == VideoStatus.Published
                && v.Visibility == VideoVisibility.Public);
        }

        return videos.Where(v =>
            v.UserId == viewerId
            || (v.Status == VideoStatus.Published
                && (v.Visibility == VideoVisibility.Public
                    || (v.Visibility == VideoVisibility.FollowersOnly
                        && db.Follows.Any(f =>
                            f.FollowerId == viewerId && f.FollowingId == v.UserId)))));
    }

    /// <summary>Newest first, which is the order every listing in FunSend uses.</summary>
    public static IQueryable<Video> Newest(this IQueryable<Video> videos)
        => videos.OrderByDescending(v => v.CreatedAt).ThenByDescending(v => v.Id);

    /// <summary>Projects to the card shown in a listing.</summary>
    public static IQueryable<VideoListItem> ToListItems(this IQueryable<Video> videos)
        => videos.Select(v => new VideoListItem
        {
            Id = v.Id,
            Title = v.Title,
            StoredFileName = v.StoredFileName,
            DurationSeconds = v.DurationSeconds,
            CreatedAt = v.CreatedAt,
            Visibility = v.Visibility,
            Status = v.Status,
            AuthorId = v.UserId,
            AuthorDisplayName = v.User!.DisplayName,
            CategoryName = v.Category!.Name,
            LikeCount = v.Likes.Count,
            CommentCount = v.Comments.Count(c => !c.IsRemoved)
        });
}
