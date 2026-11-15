using FunSend.Data;
using FunSend.Data.Entities;
using FunSend.Data.Enums;
using FunSend.Services.Notifications;
using FunSend.Services.Videos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace FunSend.Tests;

/// <summary>
/// A fresh in-memory database per test, with small helpers for the rows the
/// service tests need. Every test gets its own database name, so the tests do
/// not see each other and can run in any order.
/// </summary>
public abstract class DatabaseTestBase
{
    protected FunSendDbContext Db { get; private set; } = null!;

    [SetUp]
    public void SetUpDatabase()
    {
        var options = new DbContextOptionsBuilder<FunSendDbContext>()
            .UseInMemoryDatabase($"funsend-{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        Db = new FunSendDbContext(options);
    }

    [TearDown]
    public void TearDownDatabase() => Db.Dispose();

    protected ApplicationUser AddUser(
        string id,
        string? displayName = null,
        bool isBlocked = false)
    {
        var user = new ApplicationUser
        {
            Id = id,
            UserName = $"{id}@funsend.test",
            Email = $"{id}@funsend.test",
            DisplayName = displayName ?? id,
            IsBlocked = isBlocked,
            CreatedAt = DateTime.UtcNow
        };

        Db.Users.Add(user);
        Db.SaveChanges();

        return user;
    }

    protected Category AddCategory(string name = "Comedy")
    {
        var category = new Category { Name = name };

        Db.Categories.Add(category);
        Db.SaveChanges();

        return category;
    }

    protected Video AddVideo(
        string userId,
        int categoryId,
        VideoVisibility visibility = VideoVisibility.Public,
        VideoStatus status = VideoStatus.Published,
        string title = "A video")
    {
        var video = new Video
        {
            Title = title,
            StoredFileName = $"{Guid.NewGuid():N}.mp4",
            FileSizeBytes = 2048,
            DurationSeconds = 30,
            Visibility = visibility,
            Status = status,
            UserId = userId,
            CategoryId = categoryId,
            CreatedAt = DateTime.UtcNow
        };

        Db.Videos.Add(video);
        Db.SaveChanges();

        return video;
    }

    protected void AddFollow(string followerId, string followingId)
    {
        Db.Follows.Add(new Follow
        {
            FollowerId = followerId,
            FollowingId = followingId,
            CreatedAt = DateTime.UtcNow
        });

        Db.SaveChanges();
    }

    protected static VideoUploadLimits DefaultLimits => new()
    {
        MaxFileSizeBytes = 1024 * 1024,
        MaxDurationSeconds = 180,
        AllowedExtensions = [".mp4", ".webm", ".mov"],
        AllowedContentTypes = ["video/mp4", "video/webm", "video/quicktime"]
    };

    protected static IVideoUploadValidator BuildValidator(VideoUploadLimits? limits = null)
        => new VideoUploadValidator(Options.Create(limits ?? DefaultLimits));

    protected INotificationService BuildNotifications() => new NotificationService(Db);
}
