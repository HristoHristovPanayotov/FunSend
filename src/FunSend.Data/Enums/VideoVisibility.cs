namespace FunSend.Data.Enums;

/// <summary>Who is allowed to see a video, chosen by its author.</summary>
public enum VideoVisibility
{
    /// <summary>Visible to everyone, guests included.</summary>
    Public = 0,

    /// <summary>Visible to the author and to the users who follow the author.</summary>
    FollowersOnly = 1,

    /// <summary>Visible to the author only.</summary>
    Private = 2
}
