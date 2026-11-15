using Microsoft.AspNetCore.Identity;

namespace FunSend.Data.Entities;

/// <summary>
/// A FunSend user. Identity supplies the credentials and the role membership;
/// the profile fields below are the FunSend additions to AspNetUsers.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public const int DisplayNameMaxLength = 60;
    public const int BioMaxLength = 400;

    /// <summary>Name shown on the profile and next to every video.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Short free-text description of the author.</summary>
    public string? Bio { get; set; }

    /// <summary>File name of the avatar inside the avatar storage folder.</summary>
    public string? AvatarFileName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Set by an administrator; a blocked user cannot sign in or upload.</summary>
    public bool IsBlocked { get; set; }

    public ICollection<Video> Videos { get; set; } = new List<Video>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public ICollection<SavedVideo> SavedVideos { get; set; } = new List<SavedVideo>();

    /// <summary>Rows where this user is the one doing the following.</summary>
    public ICollection<Follow> Following { get; set; } = new List<Follow>();

    /// <summary>Rows where this user is the one being followed.</summary>
    public ICollection<Follow> Followers { get; set; } = new List<Follow>();

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
