namespace FunSend.Data.Entities;

/// <summary>
/// <see cref="FollowerId"/> follows <see cref="FollowingId"/>. The composite key
/// prevents duplicates and a check constraint prevents following oneself.
/// </summary>
public class Follow
{
    public string FollowerId { get; set; } = string.Empty;

    public ApplicationUser? Follower { get; set; }

    public string FollowingId { get; set; } = string.Empty;

    public ApplicationUser? Following { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
