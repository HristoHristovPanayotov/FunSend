using System.ComponentModel.DataAnnotations;
using FunSend.Data.Entities;
using FunSend.Data.Enums;
using FunSend.Services.Videos;

namespace FunSend.Web.Models;

/// <summary>A listing page: the feed, a search result, the saved videos.</summary>
public class FeedViewModel
{
    public string Heading { get; init; } = "Feed";

    public string? Subheading { get; init; }

    public PagedResult<VideoListItem> Videos { get; init; } =
        PagedResult<VideoListItem>.Empty(new PageRequest());

    /// <summary>Action the pager links back to.</summary>
    public string PagerAction { get; init; } = "Index";

    public string PagerController { get; init; } = "Home";

    public IDictionary<string, string?> PagerRouteValues { get; init; } =
        new Dictionary<string, string?>();

    public string EmptyMessage { get; init; } = "There is nothing here yet.";
}

/// <summary>
/// The upload form. Client-side validation comes from these annotations and
/// the service layer checks the same limits again on the server.
/// </summary>
public class VideoUploadViewModel
{
    [Required(ErrorMessage = "A title is required.")]
    [StringLength(Video.TitleMaxLength, MinimumLength = 3,
        ErrorMessage = "The title must be between {2} and {1} characters.")]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [StringLength(Video.DescriptionMaxLength,
        ErrorMessage = "The description cannot be longer than {1} characters.")]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Choose a category.")]
    [Range(1, int.MaxValue, ErrorMessage = "Choose a category.")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Display(Name = "Who can see it")]
    public VideoVisibility Visibility { get; set; } = VideoVisibility.Public;

    [StringLength(200)]
    [Display(Name = "Tags")]
    public string? Hashtags { get; set; }

    [Required(ErrorMessage = "Choose a video file.")]
    [Display(Name = "Video file")]
    public IFormFile? File { get; set; }

    /// <summary>
    /// Filled in by the browser from the chosen file. The server does not
    /// trust it on its own; it is validated against the configured maximum.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "The length of the video could not be read.")]
    public int DurationSeconds { get; set; }

    public IReadOnlyList<Category> Categories { get; set; } = [];

    public double MaxFileSizeMegabytes { get; set; }

    public int MaxDurationSeconds { get; set; }

    public string AllowedExtensions { get; set; } = string.Empty;
}

/// <summary>The details page with the player and everything around it.</summary>
public class VideoDetailsViewModel
{
    public required VideoDetails Video { get; init; }

    public bool ViewerIsSignedIn { get; init; }

    public bool ViewerIsAdmin { get; init; }
}

/// <summary>The search form together with its results.</summary>
public class SearchViewModel
{
    [Display(Name = "Words in the title")]
    [StringLength(100)]
    public string? Term { get; set; }

    [Display(Name = "Category")]
    public int? CategoryId { get; set; }

    [Display(Name = "Tag")]
    [StringLength(Data.Entities.Hashtag.NameMaxLength)]
    public string? Hashtag { get; set; }

    public int Page { get; set; } = 1;

    public IReadOnlyList<Category> Categories { get; set; } = [];

    public PagedResult<VideoListItem>? Results { get; set; }

    public bool HasQuery =>
        !string.IsNullOrWhiteSpace(Term)
        || CategoryId is > 0
        || !string.IsNullOrWhiteSpace(Hashtag);
}
