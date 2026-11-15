using FunSend.Data.Enums;
using FunSend.Services.Common;
using FunSend.Services.Social;
using FunSend.Services.Videos;

namespace FunSend.Tests.Social;

[TestFixture]
public class SocialServiceTests : DatabaseTestBase
{
    private SocialService _service = null!;

    [SetUp]
    public void SetUpService() => _service = new SocialService(Db, BuildNotifications());

    [Test]
    public async Task Following_twice_takes_the_follow_back()
    {
        var author = AddUser("author");
        var viewer = AddUser("viewer");

        var first = await _service.ToggleFollowAsync(viewer.Id, author.Id);
        var second = await _service.ToggleFollowAsync(viewer.Id, author.Id);

        Assert.Multiple(() =>
        {
            Assert.That(first.Value!.ViewerFollows, Is.True);
            Assert.That(first.Value.FollowerCount, Is.EqualTo(1));
            Assert.That(second.Value!.ViewerFollows, Is.False);
            Assert.That(second.Value.FollowerCount, Is.Zero);
        });
    }

    [Test]
    public async Task A_new_follower_is_notified_once()
    {
        var author = AddUser("author");
        var viewer = AddUser("viewer", "Viewer One");

        await _service.ToggleFollowAsync(viewer.Id, author.Id);
        await _service.ToggleFollowAsync(viewer.Id, author.Id);

        var notifications = Db.Notifications.ToList();

        Assert.Multiple(() =>
        {
            Assert.That(notifications, Has.Count.EqualTo(1));
            Assert.That(notifications[0].Type, Is.EqualTo(NotificationType.NewFollower));
            Assert.That(notifications[0].UserId, Is.EqualTo(author.Id));
        });
    }

    [Test]
    public async Task Following_yourself_is_refused()
    {
        var user = AddUser("user");

        var result = await _service.ToggleFollowAsync(user.Id, user.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorKind, Is.EqualTo(ServiceErrorKind.Validation));
            Assert.That(Db.Follows.Count(), Is.Zero);
        });
    }

    [Test]
    public async Task Following_somebody_who_does_not_exist_is_NotFound()
    {
        var viewer = AddUser("viewer");

        var result = await _service.ToggleFollowAsync(viewer.Id, "ghost");

        Assert.That(result.ErrorKind, Is.EqualTo(ServiceErrorKind.NotFound));
    }

    [Test]
    public async Task Saving_twice_removes_the_video_from_the_saved_list()
    {
        var category = AddCategory();
        var author = AddUser("author");
        var viewer = AddUser("viewer");
        var video = AddVideo(author.Id, category.Id);

        var first = await _service.ToggleSaveAsync(video.Id, viewer.Id);
        var second = await _service.ToggleSaveAsync(video.Id, viewer.Id);

        Assert.Multiple(() =>
        {
            Assert.That(first.Value!.SavedByViewer, Is.True);
            Assert.That(second.Value!.SavedByViewer, Is.False);
            Assert.That(Db.SavedVideos.Count(), Is.Zero);
        });
    }

    [Test]
    public async Task A_video_the_viewer_may_not_see_cannot_be_saved()
    {
        var category = AddCategory();
        var author = AddUser("author");
        var stranger = AddUser("stranger");
        var video = AddVideo(author.Id, category.Id, VideoVisibility.Private);

        var result = await _service.ToggleSaveAsync(video.Id, stranger.Id);

        Assert.That(result.ErrorKind, Is.EqualTo(ServiceErrorKind.NotFound));
    }

    [Test]
    public async Task The_saved_list_is_newest_save_first()
    {
        var category = AddCategory();
        var author = AddUser("author");
        var viewer = AddUser("viewer");
        var first = AddVideo(author.Id, category.Id, title: "Saved first");
        var second = AddVideo(author.Id, category.Id, title: "Saved second");

        await _service.ToggleSaveAsync(first.Id, viewer.Id);
        await _service.ToggleSaveAsync(second.Id, viewer.Id);

        // The two saves can land in the same millisecond, so make the order explicit.
        var older = Db.SavedVideos.Single(s => s.VideoId == first.Id);
        older.CreatedAt = DateTime.UtcNow.AddMinutes(-5);
        Db.SaveChanges();

        var saved = await _service.GetSavedAsync(viewer.Id, new PageRequest());

        Assert.Multiple(() =>
        {
            Assert.That(saved.TotalCount, Is.EqualTo(2));
            Assert.That(saved.Items[0].Title, Is.EqualTo("Saved second"));
        });
    }

    [Test]
    public async Task The_profile_counts_videos_followers_and_likes()
    {
        var category = AddCategory();
        var author = AddUser("author", "The Author");
        var viewer = AddUser("viewer");
        var video = AddVideo(author.Id, category.Id);

        AddFollow(viewer.Id, author.Id);
        Db.Likes.Add(new Data.Entities.Like { VideoId = video.Id, UserId = viewer.Id });
        Db.SaveChanges();

        var result = await _service.GetProfileAsync(author.Id, viewer.Id);
        var profile = result.Value!;

        Assert.Multiple(() =>
        {
            Assert.That(profile.DisplayName, Is.EqualTo("The Author"));
            Assert.That(profile.VideoCount, Is.EqualTo(1));
            Assert.That(profile.FollowerCount, Is.EqualTo(1));
            Assert.That(profile.TotalLikes, Is.EqualTo(1));
            Assert.That(profile.ViewerFollows, Is.True);
            Assert.That(profile.IsOwnProfile, Is.False);
        });
    }

    [Test]
    public async Task The_profile_of_the_viewer_is_marked_as_their_own()
    {
        var user = AddUser("user");

        var result = await _service.GetProfileAsync(user.Id, user.Id);

        Assert.That(result.Value!.IsOwnProfile, Is.True);
    }
}
