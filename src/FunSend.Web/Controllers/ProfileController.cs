using FunSend.Data.Constants;
using FunSend.Data.Entities;
using FunSend.Services.Common;
using FunSend.Services.Social;
using FunSend.Services.Videos;
using FunSend.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FunSend.Web.Controllers;

/// <summary>An author page with the profile header, their videos and the follow button.</summary>
public class ProfileController : Controller
{
    private readonly ISocialService _social;
    private readonly IVideoService _videos;
    private readonly UserManager<ApplicationUser> _users;

    public ProfileController(
        ISocialService social,
        IVideoService videos,
        UserManager<ApplicationUser> users)
    {
        _social = social;
        _videos = videos;
        _users = users;
    }

    public async Task<IActionResult> Index(
        string id,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var viewerId = _users.GetUserId(User);
        var isAdmin = User.IsInRole(RoleNames.Admin);

        var profile = await _social.GetProfileAsync(id, viewerId, cancellationToken);

        if (!profile.Succeeded || profile.Value is null)
        {
            return NotFound();
        }

        var videos = await _videos.GetByAuthorAsync(
            id,
            viewerId,
            new PageRequest(page),
            isAdmin,
            cancellationToken);

        return View(new ProfileViewModel
        {
            Profile = profile.Value,
            Videos = videos,
            ViewerIsSignedIn = viewerId is not null,
            ViewerIsAdmin = isAdmin
        });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Follow(string id, CancellationToken cancellationToken = default)
    {
        var result = await _social.ToggleFollowAsync(
            _users.GetUserId(User)!,
            id,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorKind == ServiceErrorKind.NotFound)
            {
                return NotFound();
            }

            TempData["Error"] = result.FirstError;
        }
        else
        {
            TempData["Success"] = result.Value!.ViewerFollows
                ? "You are now following this author."
                : "You stopped following this author.";
        }

        return RedirectToAction(nameof(Index), new { id });
    }
}
