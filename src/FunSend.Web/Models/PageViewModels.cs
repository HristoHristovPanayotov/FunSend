using System.ComponentModel.DataAnnotations;
using FunSend.Services.Notifications;
using FunSend.Services.Social;
using FunSend.Services.Statistics;
using FunSend.Services.Videos;

namespace FunSend.Web.Models;

/// <summary>A profile: the header, then the videos of that author.</summary>
public class ProfileViewModel
{
    public required ProfileSummary Profile { get; init; }

    public PagedResult<VideoListItem> Videos { get; init; } =
        PagedResult<VideoListItem>.Empty(new PageRequest());

    public bool ViewerIsSignedIn { get; init; }

    public bool ViewerIsAdmin { get; init; }
}

/// <summary>The notification list.</summary>
public class NotificationsViewModel
{
    public PagedResult<NotificationItem> Notifications { get; init; } =
        PagedResult<NotificationItem>.Empty(new PageRequest());

    public int UnreadCount { get; init; }

    public bool UnreadOnly { get; init; }
}

/// <summary>Everything on the administrator dashboard.</summary>
public class AdminDashboardViewModel
{
    public required PlatformStatistics Statistics { get; init; }

    public IReadOnlyList<VideoViewCount> MostWatched { get; init; } = [];

    public IReadOnlyList<CreatorActivity> MostActiveCreators { get; init; } = [];

    public IReadOnlyList<DailyViewCount> ViewsPerDay { get; init; } = [];

    /// <summary>The largest daily figure, so the bars can be scaled.</summary>
    public int PeakDailyViews => ViewsPerDay.Count == 0 ? 0 : ViewsPerDay.Max(d => d.Views);
}

/// <summary>The reason a moderator gives when taking a video down.</summary>
public class ModerationViewModel
{
    public int VideoId { get; set; }

    [Required(ErrorMessage = "Give a reason.")]
    [StringLength(200, MinimumLength = 3,
        ErrorMessage = "The reason must be between {2} and {1} characters.")]
    [Display(Name = "Reason")]
    public string Reason { get; set; } = string.Empty;
}
