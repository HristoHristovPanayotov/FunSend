using FunSend.Data.Constants;
using FunSend.Data.Entities;
using FunSend.Services.Videos;
using FunSend.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FunSend.Web.Controllers;

/// <summary>The two feeds: everything the viewer may see, and the followed authors.</summary>
public class HomeController : Controller
{
    private readonly IVideoService _videos;
    private readonly UserManager<ApplicationUser> _users;

    public HomeController(IVideoService videos, UserManager<ApplicationUser> users)
    {
        _videos = videos;
        _users = users;
    }

    /// <summary>The home feed. Open to guests, who see public videos only.</summary>
    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        var viewerId = _users.GetUserId(User);
        var isAdmin = User.IsInRole(RoleNames.Admin);

        var videos = await _videos.GetFeedAsync(
            viewerId,
            new PageRequest(page),
            isAdmin,
            cancellationToken);

        return View(new FeedViewModel
        {
            Heading = "Short videos",
            Subheading = "Everything that has just been uploaded.",
            Videos = videos,
            PagerController = "Home",
            PagerAction = nameof(Index),
            EmptyMessage = "No videos yet. Be the first to upload one."
        });
    }

    /// <summary>The personal feed built from the authors the viewer follows.</summary>
    [Authorize]
    public async Task<IActionResult> Following(int page = 1, CancellationToken cancellationToken = default)
    {
        var viewerId = _users.GetUserId(User)!;

        var videos = await _videos.GetFollowingFeedAsync(
            viewerId,
            new PageRequest(page),
            cancellationToken);

        return View("Index", new FeedViewModel
        {
            Heading = "Following",
            Subheading = "Videos from the authors you follow.",
            Videos = videos,
            PagerController = "Home",
            PagerAction = nameof(Following),
            EmptyMessage = "You do not follow anybody yet, or they have not uploaded anything."
        });
    }
}
