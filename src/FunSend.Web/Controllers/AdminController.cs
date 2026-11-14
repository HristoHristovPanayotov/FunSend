using FunSend.Data.Constants;
using FunSend.Data.Entities;
using FunSend.Services.Statistics;
using FunSend.Services.Videos;
using FunSend.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FunSend.Web.Controllers;

/// <summary>
/// The administrator dashboard: the statistics and the two moderation actions
/// that phase one covers. The role check sits on the controller, so no action
/// can be reached without it.
/// </summary>
[Authorize(Roles = RoleNames.Admin)]
public class AdminController : Controller
{
    private readonly IStatisticsService _statistics;
    private readonly IVideoService _videos;
    private readonly UserManager<ApplicationUser> _users;

    public AdminController(
        IStatisticsService statistics,
        IVideoService videos,
        UserManager<ApplicationUser> users)
    {
        _statistics = statistics;
        _videos = videos;
        _users = users;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
        => View(new AdminDashboardViewModel
        {
            Statistics = await _statistics.GetPlatformStatisticsAsync(cancellationToken),
            MostWatched = await _statistics.GetMostWatchedAsync(10, cancellationToken),
            MostActiveCreators = await _statistics.GetMostActiveCreatorsAsync(10, cancellationToken),
            ViewsPerDay = await _statistics.GetViewsPerDayAsync(14, cancellationToken)
        });

    /// <summary>Takes a video down without deleting it, and tells its author why.</summary>
    [HttpPost]
    public async Task<IActionResult> RemoveVideo(
        ModerationViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Give a reason for taking the video down.";
            return RedirectToAction("Details", "Videos", new { id = model.VideoId });
        }

        var result = await _videos.RemoveByModeratorAsync(
            model.VideoId,
            _users.GetUserId(User)!,
            model.Reason,
            cancellationToken);

        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? "The video was taken down and its author was notified."
            : result.FirstError;

        return RedirectToAction("Details", "Videos", new { id = model.VideoId });
    }

    /// <summary>
    /// Blocks or unblocks a user. Blocking also locks the account out and
    /// renews the security stamp, which ends the session that user already has.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ToggleBlock(string id, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByIdAsync(id);

        if (user is null)
        {
            return NotFound();
        }

        if (user.Id == _users.GetUserId(User))
        {
            TempData["Error"] = "You cannot block your own account.";
            return RedirectToAction("Index", "Profile", new { id });
        }

        user.IsBlocked = !user.IsBlocked;

        await _users.SetLockoutEndDateAsync(
            user,
            user.IsBlocked ? DateTimeOffset.MaxValue : null);

        await _users.UpdateAsync(user);
        await _users.UpdateSecurityStampAsync(user);

        TempData["Success"] = user.IsBlocked
            ? "The user is blocked and signed out."
            : "The user is unblocked.";

        return RedirectToAction("Index", "Profile", new { id });
    }
}
