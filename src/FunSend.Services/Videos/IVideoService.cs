using FunSend.Services.Common;

namespace FunSend.Services.Videos;

/// <summary>
/// Everything the application does with videos. The controllers call only this
/// interface, which is why the rules below can be tested without a web server.
/// </summary>
public interface IVideoService
{
    /// <summary>The home feed: every video the viewer may see, newest first.</summary>
    Task<PagedResult<VideoListItem>> GetFeedAsync(
        string? viewerId,
        PageRequest page,
        bool isAdmin = false,
        CancellationToken cancellationToken = default);

    /// <summary>The personal feed: videos by the authors the viewer follows.</summary>
    Task<PagedResult<VideoListItem>> GetFollowingFeedAsync(
        string viewerId,
        PageRequest page,
        CancellationToken cancellationToken = default);

    /// <summary>The videos of one author that the viewer may see.</summary>
    Task<PagedResult<VideoListItem>> GetByAuthorAsync(
        string authorId,
        string? viewerId,
        PageRequest page,
        bool isAdmin = false,
        CancellationToken cancellationToken = default);

    /// <summary>Search by title, category or tag, inside what the viewer may see.</summary>
    Task<PagedResult<VideoListItem>> SearchAsync(
        string? term,
        int? categoryId,
        string? hashtag,
        string? viewerId,
        PageRequest page,
        bool isAdmin = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// One video with everything the details page needs, or NotFound when it
    /// does not exist or the viewer may not see it.
    /// </summary>
    Task<ServiceResult<VideoDetails>> GetDetailsAsync(
        int videoId,
        string? viewerId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the upload, writes the file through <see cref="Storage.IVideoStorage"/>
    /// and saves the row. The file is removed again when the row cannot be saved.
    /// </summary>
    Task<ServiceResult<int>> UploadAsync(
        VideoUploadRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a video with its file. Allowed for the author and for an
    /// administrator; anybody else gets Forbidden.
    /// </summary>
    Task<ServiceResult> DeleteAsync(
        int videoId,
        string userId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a like, or removes it when it is already there. Returns the new
    /// like count and whether the viewer now likes the video.
    /// </summary>
    Task<ServiceResult<LikeOutcome>> ToggleLikeAsync(
        int videoId,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>Records one playback for the administrator statistics.</summary>
    Task<ServiceResult> RecordViewAsync(
        int videoId,
        string? viewerId,
        CancellationToken cancellationToken = default);

    /// <summary>Takes a video down (moderation) without deleting the row.</summary>
    Task<ServiceResult> RemoveByModeratorAsync(
        int videoId,
        string moderatorId,
        string reason,
        CancellationToken cancellationToken = default);
}

/// <summary>Result of toggling a like.</summary>
public sealed record LikeOutcome(bool LikedByViewer, int LikeCount);
