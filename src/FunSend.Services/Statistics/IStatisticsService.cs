using FunSend.Services.Common;

namespace FunSend.Services.Statistics;

/// <summary>The counters shown at the top of the administrator dashboard.</summary>
public sealed record PlatformStatistics
{
    public int UserCount { get; init; }
    public int BlockedUserCount { get; init; }
    public int VideoCount { get; init; }
    public int RemovedVideoCount { get; init; }
    public int CommentCount { get; init; }
    public int LikeCount { get; init; }
    public int ViewCount { get; init; }
    public int ViewsLast7Days { get; init; }
    public int PendingReportCount { get; init; }
}

/// <summary>A video in the most-watched table.</summary>
public sealed record VideoViewCount(int VideoId, string Title, string AuthorDisplayName, int Views);

/// <summary>An author in the most-active table.</summary>
public sealed record CreatorActivity(string UserId, string DisplayName, int Videos, int Views, int Likes);

/// <summary>Views on one day, for the dashboard chart.</summary>
public sealed record DailyViewCount(DateOnly Day, int Views);

/// <summary>
/// Read-only statistics for the administrator. Every method counts in the
/// database rather than loading rows, so the dashboard stays cheap as the
/// tables grow.
/// </summary>
public interface IStatisticsService
{
    Task<PlatformStatistics> GetPlatformStatisticsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VideoViewCount>> GetMostWatchedAsync(
        int count = 10,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CreatorActivity>> GetMostActiveCreatorsAsync(
        int count = 10,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DailyViewCount>> GetViewsPerDayAsync(
        int days = 14,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The counters an author sees on their own profile. Forbidden when one
    /// user asks for another user's numbers.
    /// </summary>
    Task<ServiceResult<CreatorActivity>> GetCreatorStatisticsAsync(
        string authorId,
        string requestingUserId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default);
}
