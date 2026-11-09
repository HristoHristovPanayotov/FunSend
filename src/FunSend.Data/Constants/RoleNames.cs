namespace FunSend.Data.Constants;

/// <summary>
/// The three roles from the project specification. Kept as constants so that
/// seeding, authorisation attributes and tests cannot drift apart.
/// </summary>
public static class RoleNames
{
    public const string Viewer = "Viewer";
    public const string Creator = "Creator";
    public const string Admin = "Admin";

    public static readonly string[] All = [Viewer, Creator, Admin];
}
