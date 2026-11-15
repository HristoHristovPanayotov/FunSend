using FunSend.Data;
using FunSend.Data.Enums;
using FunSend.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace FunSend.Services.Statistics;

/// <inheritdoc cref="IStatisticsService"/>
public class StatisticsService : IStatisticsService
{
    private readonly FunSendDbContext _db;

    public StatisticsService(FunSendDbContext db)
    {
        _db = db;
    }

    public async Task<PlatformStatistics> GetPlatformStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        var weekAgo = DateTime.UtcNow.AddDays(-7);

        return new PlatformStatistics
        {
            UserCount = await _db.Users.CountAsync(cancellationToken),
            BlockedUserCount = await _db.Users.CountAsync(u => u.IsBlocked, cancellationToken),
            VideoCount = await _db.Videos.CountAsync(cancellationToken),
            RemovedVideoCount = await _db.Videos
                .CountAsync(v => v.Status == VideoStatus.Removed, cancellationToken),
            CommentCount = await _db.Comments.CountAsync(c => !c.IsRemoved, cancellationToken),
            LikeCount = await _db.Likes.CountAsync(cancellationToken),
            ViewCount = await _db.VideoViews.CountAsync(cancellationToken),
            ViewsLast7Days = await _db.VideoViews
                .CountAsync(v => v.ViewedAt >= weekAgo, cancellationToken),
            PendingReportCount = await _db.Reports
                .CountAsync(r => r.Status == ReportStatus.Pending, cancellationToken)
        };
    }

    public async Task<IReadOnlyList<VideoViewCount>> GetMostWatchedAsync(
        int count = 10,
        CancellationToken cancellationToken = default)
        => await _db.Videos
            .AsNoTracking()
            .OrderByDescending(v => v.Views.Count)
            .ThenByDescending(v => v.Id)
            .Take(Math.Clamp(count, 1, 50))
            .Select(v => new VideoViewCount(
                v.Id,
                v.Title,
                v.User!.DisplayName,
                v.Views.Count))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CreatorActivity>> GetMostActiveCreatorsAsync(
        int count = 10,
        CancellationToken cancellationToken = default)
        => await _db.Users
            .AsNoTracking()
            .Where(u => u.Videos.Count > 0)
            .OrderByDescending(u => u.Videos.Count)
            .ThenBy(u => u.DisplayName)
            .Take(Math.Clamp(count, 1, 50))
            .Select(u => new CreatorActivity(
                u.Id,
                u.DisplayName,
                u.Videos.Count,
                u.Videos.Sum(v => v.Views.Count),
                u.Videos.Sum(v => v.Likes.Count)))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DailyViewCount>> GetViewsPerDayAsync(
        int days = 14,
        CancellationToken cancellationToken = default)
    {
        var window = Math.Clamp(days, 1, 90);
        var from = DateTime.UtcNow.Date.AddDays(-(window - 1));

        var counted = await _db.VideoViews
            .AsNoTracking()
            .Where(v => v.ViewedAt >= from)
            .GroupBy(v => v.ViewedAt.Date)
            .Select(g => new { Day = g.Key, Views = g.Count() })
            .ToListAsync(cancellationToken);

        // Days with no views have to appear in the chart as zero.
        return Enumerable.Range(0, window)
            .Select(offset =>
            {
                var day = from.AddDays(offset);
                var match = counted.FirstOrDefault(c => c.Day == day);

                return new DailyViewCount(DateOnly.FromDateTime(day), match?.Views ?? 0);
            })
            .ToList();
    }

    public async Task<ServiceResult<CreatorActivity>> GetCreatorStatisticsAsync(
        string authorId,
        string requestingUserId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        if (!isAdmin && authorId != requestingUserId)
        {
            return ServiceResult<CreatorActivity>.Forbidden(
                "You can only see the statistics of your own videos.");
        }

        var activity = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == authorId)
            .Select(u => new CreatorActivity(
                u.Id,
                u.DisplayName,
                u.Videos.Count,
                u.Videos.Sum(v => v.Views.Count),
                u.Videos.Sum(v => v.Likes.Count)))
            .FirstOrDefaultAsync(cancellationToken);

        return activity is null
            ? ServiceResult<CreatorActivity>.NotFound("The user does not exist.")
            : ServiceResult<CreatorActivity>.Success(activity);
    }
}
