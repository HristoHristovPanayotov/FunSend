using FunSend.Services.Common;
using FunSend.Services.Videos;

namespace FunSend.Services.Social;

/// <summary>Result of following or unfollowing an author.</summary>
public sealed record FollowOutcome(bool ViewerFollows, int FollowerCount);

/// <summary>Result of saving or unsaving a video.</summary>
public sealed record SaveOutcome(bool SavedByViewer, int SavedCount);

/// <summary>What the profile page shows above the video list.</summary>
public sealed record ProfileSummary
{
    public string UserId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Bio { get; init; }
    public string? AvatarFileName { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsBlocked { get; init; }
    public int VideoCount { get; init; }
    public int FollowerCount { get; init; }
    public int FollowingCount { get; init; }
    public int TotalLikes { get; init; }
    public bool ViewerFollows { get; init; }
    public bool IsOwnProfile { get; init; }
}

/// <summary>
/// The relations between users: following an author and putting a video aside.
/// Both are toggles, because that is how the interface uses them.
/// </summary>
public interface ISocialService
{
    /// <summary>
    /// Follows the author, or unfollows when the relation is already there.
    /// Following oneself is Invalid, and a missing author is NotFound.
    /// </summary>
    Task<ServiceResult<FollowOutcome>> ToggleFollowAsync(
        string followerId,
        string authorId,
        CancellationToken cancellationToken = default);

    /// <summary>Saves the video for later, or removes it from the saved list.</summary>
    Task<ServiceResult<SaveOutcome>> ToggleSaveAsync(
        int videoId,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>The saved videos of one user, newest save first.</summary>
    Task<PagedResult<VideoListItem>> GetSavedAsync(
        string userId,
        PageRequest page,
        CancellationToken cancellationToken = default);

    /// <summary>The profile header of one user, as the viewer sees it.</summary>
    Task<ServiceResult<ProfileSummary>> GetProfileAsync(
        string authorId,
        string? viewerId,
        CancellationToken cancellationToken = default);
}
