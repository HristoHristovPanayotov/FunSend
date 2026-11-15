using FunSend.Data;
using FunSend.Data.Entities;
using FunSend.Data.Enums;
using FunSend.Services.Common;
using FunSend.Services.Notifications;
using FunSend.Services.Videos;
using Microsoft.EntityFrameworkCore;

namespace FunSend.Services.Social;

/// <inheritdoc cref="ISocialService"/>
public class SocialService : ISocialService
{
    private readonly FunSendDbContext _db;
    private readonly INotificationService _notifications;

    public SocialService(FunSendDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<ServiceResult<FollowOutcome>> ToggleFollowAsync(
        string followerId,
        string authorId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(followerId))
        {
            return ServiceResult<FollowOutcome>.Forbidden("Sign in to follow an author.");
        }

        if (followerId == authorId)
        {
            return ServiceResult<FollowOutcome>.Invalid("You cannot follow yourself.");
        }

        var author = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == authorId, cancellationToken);

        if (author is null)
        {
            return ServiceResult<FollowOutcome>.NotFound("The author does not exist.");
        }

        var follow = await _db.Follows.FirstOrDefaultAsync(
            f => f.FollowerId == followerId && f.FollowingId == authorId,
            cancellationToken);

        bool viewerFollows;

        if (follow is null)
        {
            _db.Follows.Add(new Follow
            {
                FollowerId = followerId,
                FollowingId = authorId,
                CreatedAt = DateTime.UtcNow
            });

            viewerFollows = true;
        }
        else
        {
            _db.Follows.Remove(follow);
            viewerFollows = false;
        }

        await _db.SaveChangesAsync(cancellationToken);

        if (viewerFollows)
        {
            var followerName = await _db.Users
                .Where(u => u.Id == followerId)
                .Select(u => u.DisplayName)
                .FirstOrDefaultAsync(cancellationToken) ?? "Someone";

            await _notifications.NotifyAsync(
                authorId,
                NotificationType.NewFollower,
                $"{followerName} started following you.",
                followerId,
                videoId: null,
                cancellationToken);
        }

        var followerCount = await _db.Follows
            .CountAsync(f => f.FollowingId == authorId, cancellationToken);

        return ServiceResult<FollowOutcome>.Success(new FollowOutcome(viewerFollows, followerCount));
    }

    public async Task<ServiceResult<SaveOutcome>> ToggleSaveAsync(
        int videoId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return ServiceResult<SaveOutcome>.Forbidden("Sign in to save a video.");
        }

        var visible = await _db.Videos
            .AsNoTracking()
            .Where(v => v.Id == videoId)
            .VisibleTo(_db, userId)
            .AnyAsync(cancellationToken);

        if (!visible)
        {
            return ServiceResult<SaveOutcome>.NotFound("The video does not exist or is not available to you.");
        }

        var saved = await _db.SavedVideos.FirstOrDefaultAsync(
            s => s.VideoId == videoId && s.UserId == userId,
            cancellationToken);

        bool savedByViewer;

        if (saved is null)
        {
            _db.SavedVideos.Add(new SavedVideo
            {
                VideoId = videoId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });

            savedByViewer = true;
        }
        else
        {
            _db.SavedVideos.Remove(saved);
            savedByViewer = false;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var savedCount = await _db.SavedVideos.CountAsync(s => s.UserId == userId, cancellationToken);

        return ServiceResult<SaveOutcome>.Success(new SaveOutcome(savedByViewer, savedCount));
    }

    public async Task<PagedResult<VideoListItem>> GetSavedAsync(
        string userId,
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return PagedResult<VideoListItem>.Empty(page);
        }

        // Ordered by when the user saved it, which is why this does not reuse
        // the newest-first helper the feeds share.
        var saved = _db.SavedVideos
            .AsNoTracking()
            .Where(s => s.UserId == userId);

        var total = await saved.CountAsync(cancellationToken);

        var items = await saved
            .OrderByDescending(s => s.CreatedAt)
            .ThenByDescending(s => s.VideoId)
            .Skip(page.Skip)
            .Take(page.Take)
            .Select(s => s.Video!)
            .VisibleTo(_db, userId)
            .ToListItems()
            .ToListAsync(cancellationToken);

        return new PagedResult<VideoListItem>(items, total, Math.Max(page.Page, 1), page.Take);
    }

    public async Task<ServiceResult<ProfileSummary>> GetProfileAsync(
        string authorId,
        string? viewerId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == authorId)
            .Select(u => new ProfileSummary
            {
                UserId = u.Id,
                DisplayName = u.DisplayName,
                Bio = u.Bio,
                AvatarFileName = u.AvatarFileName,
                CreatedAt = u.CreatedAt,
                IsBlocked = u.IsBlocked,
                VideoCount = u.Videos.Count(v => v.Status == VideoStatus.Published),
                FollowerCount = _db.Follows.Count(f => f.FollowingId == u.Id),
                FollowingCount = _db.Follows.Count(f => f.FollowerId == u.Id),
                TotalLikes = u.Videos.Sum(v => v.Likes.Count),
                ViewerFollows = viewerId != null && _db.Follows.Any(f =>
                    f.FollowerId == viewerId && f.FollowingId == u.Id),
                IsOwnProfile = viewerId != null && viewerId == u.Id
            })
            .FirstOrDefaultAsync(cancellationToken);

        return profile is null
            ? ServiceResult<ProfileSummary>.NotFound("The user does not exist.")
            : ServiceResult<ProfileSummary>.Success(profile);
    }
}
