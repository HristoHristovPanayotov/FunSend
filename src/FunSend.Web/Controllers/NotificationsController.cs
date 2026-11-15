using FunSend.Data.Entities;
using FunSend.Services.Common;
using FunSend.Services.Notifications;
using FunSend.Services.Videos;
using FunSend.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FunSend.Web.Controllers;

/// <summary>The notification list of the signed-in user.</summary>
[Authorize]
public class NotificationsController : Controller
{
    private readonly INotificationService _notifications;
    private readonly UserManager<ApplicationUser> _users;

    public NotificationsController(
        INotificationService notifications,
        UserManager<ApplicationUser> users)
    {
        _notifications = notifications;
        _users = users;
    }

    public async Task<IActionResult> Index(
        bool unreadOnly = false,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var userId = _users.GetUserId(User)!;

        var notifications = await _notifications.GetForUserAsync(
            userId,
            new PageRequest(page),
            unreadOnly,
            cancellationToken);

        return View(new NotificationsViewModel
        {
            Notifications = notifications,
            UnreadCount = await _notifications.GetUnreadCountAsync(userId, cancellationToken),
            UnreadOnly = unreadOnly
        });
    }

    [HttpPost]
    public async Task<IActionResult> MarkRead(int id, CancellationToken cancellationToken = default)
    {
        var result = await _notifications.MarkAsReadAsync(
            id,
            _users.GetUserId(User)!,
            cancellationToken);

        if (result.ErrorKind == ServiceErrorKind.Forbidden)
        {
            return Forbid();
        }

        if (result.ErrorKind == ServiceErrorKind.NotFound)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken = default)
    {
        await _notifications.MarkAllAsReadAsync(_users.GetUserId(User)!, cancellationToken);

        TempData["Success"] = "All notifications are marked as read.";
        return RedirectToAction(nameof(Index));
    }
}
