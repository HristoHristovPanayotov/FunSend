using FunSend.Data;
using FunSend.Data.Entities;
using FunSend.Data.Enums;
using FunSend.Services.Common;
using FunSend.Services.Notifications;
using FunSend.Services.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FunSend.Services.Videos;

/// <inheritdoc cref="IVideoService"/>
public class VideoService : IVideoService
{
    /// <summary>Upper bound on the tags one video can carry.</summary>
    public const int MaxHashtagsPerVideo = 10;

    private readonly FunSendDbContext _db;
    private readonly IVideoStorage _storage;
    private readonly IVideoUploadValidator _validator;
    private readonly INotificationService _notifications;
    private readonly ILogger<VideoService> _logger;

    public VideoService(
        FunSendDbContext db,
        IVideoStorage storage,
        IVideoUploadValidator validator,
        INotificationService notifications,
        ILogger<VideoService> logger)
    {
        _db = db;
        _storage = storage;
        _validator = validator;
        _notifications = notifications;
        _logger = logger;
    }

    public Task<PagedResult<VideoListItem>> GetFeedAsync(
        string? viewerId,
        PageRequest page,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
        => PageAsync(
            _db.Videos.AsNoTracking().VisibleTo(_db, viewerId, isAdmin),
            page,
            cancellationToken);

    public Task<PagedResult<VideoListItem>> GetFollowingFeedAsync(
        string viewerId,
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(viewerId))
        {
            return Task.FromResult(PagedResult<VideoListItem>.Empty(page));
        }

        var query = _db.Videos
            .AsNoTracking()
            .Where(v => _db.Follows.Any(f =>
                f.FollowerId == viewerId && f.FollowingId == v.UserId))
            .VisibleTo(_db, viewerId);

        return PageAsync(query, page, cancellationToken);
    }

    public Task<PagedResult<VideoListItem>> GetByAuthorAsync(
        string authorId,
        string? viewerId,
        PageRequest page,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Videos
            .AsNoTracking()
            .Where(v => v.UserId == authorId)
            .VisibleTo(_db, viewerId, isAdmin);

        return PageAsync(query, page, cancellationToken);
    }

    public Task<PagedResult<VideoListItem>> SearchAsync(
        string? term,
        int? categoryId,
        string? hashtag,
        string? viewerId,
        PageRequest page,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Videos.AsNoTracking().VisibleTo(_db, viewerId, isAdmin);

        if (!string.IsNullOrWhiteSpace(term))
        {
            var needle = term.Trim();
            query = query.Where(v =>
                EF.Functions.Like(v.Title, $"%{needle}%")
                || (v.Description != null && EF.Functions.Like(v.Description, $"%{needle}%")));
        }

        if (categoryId is > 0)
        {
            query = query.Where(v => v.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(hashtag))
        {
            var tag = NormaliseHashtag(hashtag);
            query = query.Where(v => v.VideoHashtags.Any(vh => vh.Hashtag!.Name == tag));
        }

        return PageAsync(query, page, cancellationToken);
    }

    public async Task<ServiceResult<VideoDetails>> GetDetailsAsync(
        int videoId,
        string? viewerId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        var details = await _db.Videos
            .AsNoTracking()
            .Where(v => v.Id == videoId)
            .VisibleTo(_db, viewerId, isAdmin)
            .Select(v => new VideoDetails
            {
                Id = v.Id,
                Title = v.Title,
                Description = v.Description,
                StoredFileName = v.StoredFileName,
                DurationSeconds = v.DurationSeconds,
                FileSizeBytes = v.FileSizeBytes,
                CreatedAt = v.CreatedAt,
                Visibility = v.Visibility,
                Status = v.Status,
                AuthorId = v.UserId,
                AuthorDisplayName = v.User!.DisplayName,
                AuthorAvatarFileName = v.User.AvatarFileName,
                CategoryId = v.CategoryId,
                CategoryName = v.Category!.Name,
                Hashtags = v.VideoHashtags
                    .Select(vh => vh.Hashtag!.Name)
                    .OrderBy(name => name)
                    .ToList(),
                LikeCount = v.Likes.Count,
                ViewCount = v.Views.Count,
                LikedByViewer = viewerId != null && v.Likes.Any(l => l.UserId == viewerId),
                SavedByViewer = viewerId != null && v.SavedBy.Any(s => s.UserId == viewerId),
                ViewerFollowsAuthor = viewerId != null && _db.Follows.Any(f =>
                    f.FollowerId == viewerId && f.FollowingId == v.UserId),
                ViewerCanDelete = isAdmin || (viewerId != null && v.UserId == viewerId)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return details is null
            ? ServiceResult<VideoDetails>.NotFound("The video does not exist or is not available to you.")
            : ServiceResult<VideoDetails>.Success(details);
    }

    public async Task<ServiceResult<int>> UploadAsync(
        VideoUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return ServiceResult<int>.Invalid("A title is required.");
        }

        var author = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (author is null)
        {
            return ServiceResult<int>.NotFound("The author does not exist.");
        }

        if (author.IsBlocked)
        {
            return ServiceResult<int>.Forbidden("A blocked user cannot upload videos.");
        }

        var categoryExists = await _db.Categories
            .AnyAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (!categoryExists)
        {
            return ServiceResult<int>.Invalid("The selected category does not exist.");
        }

        var validation = _validator.Validate(new VideoUploadDescriptor(
            request.FileName,
            request.ContentType,
            request.SizeBytes,
            request.DurationSeconds));

        if (!validation.Succeeded)
        {
            return ServiceResult<int>.Invalid([.. validation.Errors]);
        }

        var extension = _validator.GetNormalisedExtension(request.FileName);
        var storedFileName = await _storage.SaveAsync(request.Content, extension, cancellationToken);

        try
        {
            var video = new Video
            {
                Title = request.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description)
                    ? null
                    : request.Description.Trim(),
                StoredFileName = storedFileName,
                FileSizeBytes = request.SizeBytes,
                DurationSeconds = request.DurationSeconds,
                Status = VideoStatus.Published,
                Visibility = request.Visibility,
                CreatedAt = DateTime.UtcNow,
                UserId = request.UserId,
                CategoryId = request.CategoryId
            };

            await AttachHashtagsAsync(video, request.HashtagsRaw, cancellationToken);

            _db.Videos.Add(video);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} uploaded video {VideoId}.", request.UserId, video.Id);
            return ServiceResult<int>.Success(video.Id);
        }
        catch (Exception ex)
        {
            // The row did not make it, so the file must not stay behind.
            _logger.LogError(ex, "Saving the video row failed; removing {StoredFileName}.", storedFileName);
            await _storage.DeleteAsync(storedFileName, CancellationToken.None);
            throw;
        }
    }

    public async Task<ServiceResult> DeleteAsync(
        int videoId,
        string userId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        var video = await _db.Videos
            .FirstOrDefaultAsync(v => v.Id == videoId, cancellationToken);

        if (video is null)
        {
            return ServiceResult.NotFound("The video does not exist.");
        }

        if (!isAdmin && video.UserId != userId)
        {
            return ServiceResult.Forbidden("Only the author or an administrator can delete this video.");
        }

        // Reports point at comments with NO ACTION, so they go first; everything
        // else that belongs to the video is removed by the cascade.
        var commentIds = await _db.Comments
            .Where(c => c.VideoId == videoId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var reports = await _db.Reports
            .Where(r => r.VideoId == videoId
                || (r.CommentId != null && commentIds.Contains(r.CommentId.Value)))
            .ToListAsync(cancellationToken);

        _db.Reports.RemoveRange(reports);
        _db.Videos.Remove(video);
        await _db.SaveChangesAsync(cancellationToken);

        await _storage.DeleteAsync(video.StoredFileName, cancellationToken);

        _logger.LogInformation(
            "Video {VideoId} deleted by {UserId} (administrator: {IsAdmin}).",
            videoId,
            userId,
            isAdmin);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<LikeOutcome>> ToggleLikeAsync(
        int videoId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return ServiceResult<LikeOutcome>.Forbidden("Sign in to like a video.");
        }

        var video = await _db.Videos
            .AsNoTracking()
            .Where(v => v.Id == videoId)
            .VisibleTo(_db, userId)
            .Select(v => new { v.Id, v.Title, v.UserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (video is null)
        {
            return ServiceResult<LikeOutcome>.NotFound("The video does not exist or is not available to you.");
        }

        var like = await _db.Likes
            .FirstOrDefaultAsync(l => l.VideoId == videoId && l.UserId == userId, cancellationToken);

        bool likedByViewer;

        if (like is null)
        {
            _db.Likes.Add(new Like
            {
                VideoId = videoId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });

            likedByViewer = true;
        }
        else
        {
            _db.Likes.Remove(like);
            likedByViewer = false;
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Only a new like is worth telling the author about; taking one back is not.
        if (likedByViewer)
        {
            var likerName = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.DisplayName)
                .FirstOrDefaultAsync(cancellationToken) ?? "Someone";

            await _notifications.NotifyAsync(
                video.UserId,
                NotificationType.VideoLiked,
                $"{likerName} liked your video \"{video.Title}\".",
                userId,
                video.Id,
                cancellationToken);
        }

        var likeCount = await _db.Likes.CountAsync(l => l.VideoId == videoId, cancellationToken);

        return ServiceResult<LikeOutcome>.Success(new LikeOutcome(likedByViewer, likeCount));
    }

    public async Task<ServiceResult> RecordViewAsync(
        int videoId,
        string? viewerId,
        CancellationToken cancellationToken = default)
    {
        var visible = await _db.Videos
            .AsNoTracking()
            .Where(v => v.Id == videoId)
            .VisibleTo(_db, viewerId)
            .AnyAsync(cancellationToken);

        if (!visible)
        {
            return ServiceResult.NotFound("The video does not exist or is not available to you.");
        }

        _db.VideoViews.Add(new VideoView
        {
            VideoId = videoId,
            UserId = string.IsNullOrEmpty(viewerId) ? null : viewerId,
            ViewedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> RemoveByModeratorAsync(
        int videoId,
        string moderatorId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var video = await _db.Videos
            .FirstOrDefaultAsync(v => v.Id == videoId, cancellationToken);

        if (video is null)
        {
            return ServiceResult.NotFound("The video does not exist.");
        }

        if (video.Status == VideoStatus.Removed)
        {
            return ServiceResult.Conflict("The video is already taken down.");
        }

        video.Status = VideoStatus.Removed;

        var pending = await _db.Reports
            .Where(r => r.VideoId == videoId && r.Status == ReportStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var report in pending)
        {
            report.Status = ReportStatus.Accepted;
            report.ResolvedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _notifications.NotifyAsync(
            video.UserId,
            NotificationType.VideoRemoved,
            $"Your video \"{video.Title}\" was taken down by a moderator. Reason: {reason}",
            actorId: null,
            videoId: video.Id,
            cancellationToken);

        _logger.LogInformation(
            "Moderator {ModeratorId} took video {VideoId} down: {Reason}",
            moderatorId,
            videoId,
            reason);

        return ServiceResult.Success();
    }

    private static async Task<PagedResult<VideoListItem>> PageAsync(
        IQueryable<Video> query,
        PageRequest page,
        CancellationToken cancellationToken)
    {
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Newest()
            .Skip(page.Skip)
            .Take(page.Take)
            .ToListItems()
            .ToListAsync(cancellationToken);

        return new PagedResult<VideoListItem>(items, total, Math.Max(page.Page, 1), page.Take);
    }

    /// <summary>
    /// Turns the typed tags into <see cref="Hashtag"/> rows, reusing the ones
    /// that already exist so the Hashtags table never holds a duplicate.
    /// </summary>
    private async Task AttachHashtagsAsync(
        Video video,
        string? hashtagsRaw,
        CancellationToken cancellationToken)
    {
        var names = ParseHashtags(hashtagsRaw);

        if (names.Count == 0)
        {
            return;
        }

        var existing = await _db.Hashtags
            .Where(h => names.Contains(h.Name))
            .ToListAsync(cancellationToken);

        foreach (var name in names)
        {
            var hashtag = existing.FirstOrDefault(h => h.Name == name)
                ?? new Hashtag { Name = name };

            video.VideoHashtags.Add(new VideoHashtag { Hashtag = hashtag });
        }
    }

    /// <summary>Splits on commas and spaces, normalises and removes duplicates.</summary>
    public static List<string> ParseHashtags(string? hashtagsRaw)
    {
        if (string.IsNullOrWhiteSpace(hashtagsRaw))
        {
            return [];
        }

        return hashtagsRaw
            .Split([',', ' ', ';', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries)
            .Select(NormaliseHashtag)
            .Where(name => name.Length > 0)
            .Distinct()
            .Take(MaxHashtagsPerVideo)
            .ToList();
    }

    private static string NormaliseHashtag(string raw)
    {
        var name = raw.Trim().TrimStart('#').ToLowerInvariant();

        return name.Length > Hashtag.NameMaxLength
            ? name[..Hashtag.NameMaxLength]
            : name;
    }
}
