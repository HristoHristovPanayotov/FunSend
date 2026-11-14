using FunSend.Data;
using FunSend.Data.Constants;
using FunSend.Data.Entities;
using FunSend.Services.Common;
using FunSend.Services.Social;
using FunSend.Services.Storage;
using FunSend.Services.Videos;
using FunSend.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FunSend.Web.Controllers;

/// <summary>
/// Watching, uploading, liking, saving, searching and deleting videos. The
/// controller only translates between HTTP and the service layer; every rule
/// it applies comes from there.
/// </summary>
public class VideosController : Controller
{
    private readonly IVideoService _videos;
    private readonly ISocialService _social;
    private readonly IVideoStorage _storage;
    private readonly FunSendDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly VideoUploadLimits _limits;

    public VideosController(
        IVideoService videos,
        ISocialService social,
        IVideoStorage storage,
        FunSendDbContext db,
        UserManager<ApplicationUser> users,
        IOptions<VideoUploadLimits> limits)
    {
        _videos = videos;
        _social = social;
        _storage = storage;
        _db = db;
        _users = users;
        _limits = limits.Value;
    }

    /// <summary>The player page.</summary>
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        var viewerId = _users.GetUserId(User);
        var isAdmin = User.IsInRole(RoleNames.Admin);

        var result = await _videos.GetDetailsAsync(id, viewerId, isAdmin, cancellationToken);

        if (!result.Succeeded || result.Value is null)
        {
            return ToActionResult(result);
        }

        await _videos.RecordViewAsync(id, viewerId, cancellationToken);

        return View(new VideoDetailsViewModel
        {
            Video = result.Value,
            ViewerIsSignedIn = viewerId is not null,
            ViewerIsAdmin = isAdmin
        });
    }

    /// <summary>
    /// Streams the file itself. The files are kept outside wwwroot, so this is
    /// the only way to them and the visibility rule is checked for every
    /// request instead of being bypassed by a direct link.
    /// </summary>
    public async Task<IActionResult> Stream(int id, CancellationToken cancellationToken = default)
    {
        var viewerId = _users.GetUserId(User);
        var isAdmin = User.IsInRole(RoleNames.Admin);

        var video = await _db.Videos
            .AsNoTracking()
            .Where(v => v.Id == id)
            .VisibleTo(_db, viewerId, isAdmin)
            .Select(v => new { v.StoredFileName })
            .FirstOrDefaultAsync(cancellationToken);

        if (video is null)
        {
            return NotFound();
        }

        var content = await _storage.OpenReadAsync(video.StoredFileName, cancellationToken);

        if (content is null)
        {
            return NotFound();
        }

        return File(
            content,
            GetContentType(video.StoredFileName),
            enableRangeProcessing: true);
    }

    [Authorize(Roles = $"{RoleNames.Creator},{RoleNames.Admin}")]
    public async Task<IActionResult> Upload(CancellationToken cancellationToken = default)
        => View(await BuildUploadViewModelAsync(new VideoUploadViewModel(), cancellationToken));

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Creator},{RoleNames.Admin}")]
    [RequestSizeLimit(105_906_176)]
    public async Task<IActionResult> Upload(
        VideoUploadViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid || model.File is null)
        {
            return View(await BuildUploadViewModelAsync(model, cancellationToken));
        }

        await using var content = model.File.OpenReadStream();

        var result = await _videos.UploadAsync(new VideoUploadRequest
        {
            UserId = _users.GetUserId(User)!,
            Title = model.Title,
            Description = model.Description,
            CategoryId = model.CategoryId,
            Visibility = model.Visibility,
            FileName = model.File.FileName,
            ContentType = model.File.ContentType,
            SizeBytes = model.File.Length,
            DurationSeconds = model.DurationSeconds,
            Content = content,
            HashtagsRaw = model.Hashtags
        }, cancellationToken);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(await BuildUploadViewModelAsync(model, cancellationToken));
        }

        TempData["Success"] = "The video was uploaded.";
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var result = await _videos.DeleteAsync(
            id,
            _users.GetUserId(User)!,
            User.IsInRole(RoleNames.Admin),
            cancellationToken);

        if (!result.Succeeded)
        {
            return ToActionResult(result);
        }

        TempData["Success"] = "The video was deleted.";
        return RedirectToAction(nameof(Index), "Home");
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Like(int id, CancellationToken cancellationToken = default)
    {
        var result = await _videos.ToggleLikeAsync(id, _users.GetUserId(User)!, cancellationToken);

        if (!result.Succeeded)
        {
            return ToActionResult(result);
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Save(int id, CancellationToken cancellationToken = default)
    {
        var result = await _social.ToggleSaveAsync(id, _users.GetUserId(User)!, cancellationToken);

        if (!result.Succeeded)
        {
            return ToActionResult(result);
        }

        TempData["Success"] = result.Value!.SavedByViewer
            ? "The video was added to your saved list."
            : "The video was removed from your saved list.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize]
    public async Task<IActionResult> Saved(int page = 1, CancellationToken cancellationToken = default)
    {
        var videos = await _social.GetSavedAsync(
            _users.GetUserId(User)!,
            new PageRequest(page),
            cancellationToken);

        return View("~/Views/Home/Index.cshtml", new FeedViewModel
        {
            Heading = "Saved videos",
            Subheading = "The videos you put aside for later.",
            Videos = videos,
            PagerController = "Videos",
            PagerAction = nameof(Saved),
            EmptyMessage = "You have not saved any videos yet."
        });
    }

    public async Task<IActionResult> Search(
        SearchViewModel model,
        CancellationToken cancellationToken = default)
    {
        model.Categories = await GetCategoriesAsync(cancellationToken);

        if (model.HasQuery && ModelState.IsValid)
        {
            model.Results = await _videos.SearchAsync(
                model.Term,
                model.CategoryId,
                model.Hashtag,
                _users.GetUserId(User),
                new PageRequest(model.Page),
                User.IsInRole(RoleNames.Admin),
                cancellationToken);
        }

        return View(model);
    }

    private async Task<VideoUploadViewModel> BuildUploadViewModelAsync(
        VideoUploadViewModel model,
        CancellationToken cancellationToken)
    {
        model.Categories = await GetCategoriesAsync(cancellationToken);
        model.MaxFileSizeMegabytes = _limits.MaxFileSizeMegabytes;
        model.MaxDurationSeconds = _limits.MaxDurationSeconds;
        model.AllowedExtensions = string.Join(", ", _limits.AllowedExtensions);

        return model;
    }

    private async Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken)
        => await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

    /// <summary>Turns a refusal from the service layer into the matching status code.</summary>
    private IActionResult ToActionResult(ServiceResult result)
        => result.ErrorKind switch
        {
            ServiceErrorKind.NotFound => NotFound(),
            ServiceErrorKind.Forbidden => Forbid(),
            _ => BadRequest(result.FirstError)
        };

    private static string GetContentType(string storedFileName)
        => Path.GetExtension(storedFileName).ToLowerInvariant() switch
        {
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".mov" => "video/quicktime",
            _ => "application/octet-stream"
        };
}
