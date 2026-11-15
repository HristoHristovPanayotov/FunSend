using System.Text;
using FunSend.Data.Enums;
using FunSend.Services.Common;
using FunSend.Services.Videos;
using FunSend.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FunSend.Tests.Videos;

[TestFixture]
public class VideoServiceTests : DatabaseTestBase
{
    private InMemoryVideoStorage _storage = null!;
    private VideoService _service = null!;

    [SetUp]
    public void SetUpService()
    {
        _storage = new InMemoryVideoStorage();

        _service = new VideoService(
            Db,
            _storage,
            BuildValidator(),
            BuildNotifications(),
            NullLogger<VideoService>.Instance);
    }

    private static VideoUploadRequest BuildUpload(
        string userId,
        int categoryId,
        string fileName = "clip.mp4",
        long sizeBytes = 2048,
        int durationSeconds = 30,
        VideoVisibility visibility = VideoVisibility.Public,
        string? hashtags = null)
        => new()
        {
            UserId = userId,
            Title = "My clip",
            Description = "A short description",
            CategoryId = categoryId,
            Visibility = visibility,
            FileName = fileName,
            ContentType = "video/mp4",
            SizeBytes = sizeBytes,
            DurationSeconds = durationSeconds,
            Content = new MemoryStream(Encoding.UTF8.GetBytes("pretend this is a video")),
            HashtagsRaw = hashtags
        };

    // --- Upload -----------------------------------------------------------

    [Test]
    public async Task Upload_stores_the_file_and_the_row()
    {
        var category = AddCategory();
        var author = AddUser("author");

        var result = await _service.UploadAsync(BuildUpload(author.Id, category.Id));

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True, result.FirstError);
            Assert.That(_storage.Files, Has.Count.EqualTo(1));
            Assert.That(Db.Videos.Count(), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Upload_refuses_a_blocked_user()
    {
        var category = AddCategory();
        var author = AddUser("author", isBlocked: true);

        var result = await _service.UploadAsync(BuildUpload(author.Id, category.Id));

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorKind, Is.EqualTo(ServiceErrorKind.Forbidden));
            Assert.That(_storage.SaveCount, Is.Zero, "nothing should be written for a blocked user");
        });
    }

    [Test]
    public async Task Upload_refuses_a_category_that_does_not_exist()
    {
        var author = AddUser("author");

        var result = await _service.UploadAsync(BuildUpload(author.Id, categoryId: 999));

        Assert.That(result.ErrorKind, Is.EqualTo(ServiceErrorKind.Validation));
    }

    [Test]
    public async Task Upload_refuses_a_file_that_fails_validation_without_writing_it()
    {
        var category = AddCategory();
        var author = AddUser("author");

        var result = await _service.UploadAsync(
            BuildUpload(author.Id, category.Id, fileName: "clip.avi"));

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorKind, Is.EqualTo(ServiceErrorKind.Validation));
            Assert.That(_storage.SaveCount, Is.Zero);
            Assert.That(Db.Videos.Count(), Is.Zero);
        });
    }

    [Test]
    public async Task Upload_refuses_an_empty_title()
    {
        var category = AddCategory();
        var author = AddUser("author");

        var request = BuildUpload(author.Id, category.Id) with { Title = "   " };

        var result = await _service.UploadAsync(request);

        Assert.That(result.ErrorKind, Is.EqualTo(ServiceErrorKind.Validation));
    }

    [Test]
    public async Task Upload_reuses_a_hashtag_that_already_exists()
    {
        var category = AddCategory();
        var author = AddUser("author");

        await _service.UploadAsync(BuildUpload(author.Id, category.Id, hashtags: "Skate, funny"));
        await _service.UploadAsync(BuildUpload(author.Id, category.Id, hashtags: "#skate"));

        Assert.Multiple(() =>
        {
            Assert.That(Db.Hashtags.Count(), Is.EqualTo(2), "skate must not be stored twice");
            Assert.That(Db.VideoHashtags.Count(), Is.EqualTo(3));
        });
    }

    [TestCase("a, b, c", 3)]
    [TestCase("#one #two", 2)]
    [TestCase("same, same, SAME", 1)]
    [TestCase("", 0)]
    [TestCase(null, 0)]
    public void ParseHashtags_normalises_and_removes_duplicates(string? raw, int expected)
        => Assert.That(VideoService.ParseHashtags(raw), Has.Count.EqualTo(expected));

    [Test]
    public void ParseHashtags_keeps_at_most_ten()
    {
        var raw = string.Join(" ", Enumerable.Range(1, 20).Select(i => $"tag{i}"));

        Assert.That(
            VideoService.ParseHashtags(raw),
            Has.Count.EqualTo(VideoService.MaxHashtagsPerVideo));
    }

    // --- Delete -----------------------------------------------------------

    [Test]
    public async Task The_author_can_delete_their_own_video()
    {
        var category = AddCategory();
        var author = AddUser("author");
        var video = AddVideo(author.Id, category.Id);

        var result = await _service.DeleteAsync(video.Id, author.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True, result.FirstError);
            Assert.That(Db.Videos.Count(), Is.Zero);
            Assert.That(_storage.DeleteCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Another_user_cannot_delete_somebody_elses_video()
    {
        var category = AddCategory();
        var author = AddUser("author");
        var stranger = AddUser("stranger");
        var video = AddVideo(author.Id, category.Id);

        var result = await _service.DeleteAsync(video.Id, stranger.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorKind, Is.EqualTo(ServiceErrorKind.Forbidden));
            Assert.That(Db.Videos.Count(), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task An_administrator_can_delete_any_video()
    {
        var category = AddCategory();
        var author = AddUser("author");
        var admin = AddUser("admin");
        var video = AddVideo(author.Id, category.Id);

        var result = await _service.DeleteAsync(video.Id, admin.Id, isAdmin: true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True, result.FirstError);
            Assert.That(Db.Videos.Count(), Is.Zero);
        });
    }

    [Test]
    public async Task Deleting_a_video_that_does_not_exist_is_NotFound()
    {
        var user = AddUser("user");

        var result = await _service.DeleteAsync(404, user.Id);

        Assert.That(result.ErrorKind, Is.EqualTo(ServiceErrorKind.NotFound));
    }

    // --- Likes ------------------------------------------------------------

    [Test]
    public async Task Liking_twice_takes_the_like_back()
    {
        var category = AddCategory();
        var author = AddUser("author");
        var viewer = AddUser("viewer");
        var video = AddVideo(author.Id, category.Id);

        var first = await _service.ToggleLikeAsync(video.Id, viewer.Id);
        var second = await _service.ToggleLikeAsync(video.Id, viewer.Id);

        Assert.Multiple(() =>
        {
            Assert.That(first.Value!.LikedByViewer, Is.True);
            Assert.That(first.Value.LikeCount, Is.EqualTo(1));
            Assert.That(second.Value!.LikedByViewer, Is.False);
            Assert.That(second.Value.LikeCount, Is.Zero);
            Assert.That(Db.Likes.Count(), Is.Zero);
        });
    }

    [Test]
    public async Task A_like_notifies_the_author_but_taking_it_back_does_not()
    {
        var category = AddCategory();
        var author = AddUser("author");
        var viewer = AddUser("viewer", "Viewer One");
        var video = AddVideo(author.Id, category.Id);

        await _service.ToggleLikeAsync(video.Id, viewer.Id);
        await _service.ToggleLikeAsync(video.Id, viewer.Id);

        var notifications = await Db.Notifications.ToListAsync();

        Assert.Multiple(() =>
        {
            Assert.That(notifications, Has.Count.EqualTo(1));
            Assert.That(notifications[0].Type, Is.EqualTo(NotificationType.VideoLiked));
            Assert.That(notifications[0].UserId, Is.EqualTo(author.Id));
            Assert.That(notifications[0].Message, Does.Contain("Viewer One"));
        });
    }

    [Test]
    public async Task Liking_your_own_video_notifies_nobody()
    {
        var category = AddCategory();
        var author = AddUser("author");
        var video = AddVideo(author.Id, category.Id);

        await _service.ToggleLikeAsync(video.Id, author.Id);

        Assert.That(Db.Notifications.Count(), Is.Zero);
    }

    [Test]
    public async Task A_private_video_of_somebody_else_cannot_be_liked()
    {
        var category = AddCategory();
        var author = AddUser("author");
        var stranger = AddUser("stranger");
        var video = AddVideo(author.Id, category.Id, VideoVisibility.Private);

        var result = await _service.ToggleLikeAsync(video.Id, stranger.Id);

        Assert.That(result.ErrorKind, Is.EqualTo(ServiceErrorKind.NotFound));
    }

    // --- Views and moderation --------------------------------------------

    [Test]
    public async Task A_guest_view_is_recorded_without_a_user()
    {
        var category = AddCategory();
        var author = AddUser("author");
        var video = AddVideo(author.Id, category.Id);

        var result = await _service.RecordViewAsync(video.Id, viewerId: null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True, result.FirstError);
            Assert.That(Db.VideoViews.Single().UserId, Is.Null);
        });
    }

    [Test]
    public async Task Moderation_marks_the_video_removed_and_notifies_the_author()
    {
        var category = AddCategory();
        var author = AddUser("author");
        var admin = AddUser("admin");
        var video = AddVideo(author.Id, category.Id);

        var result = await _service.RemoveByModeratorAsync(video.Id, admin.Id, "Breaks the rules");

        var notification = await Db.Notifications.SingleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True, result.FirstError);
            Assert.That(Db.Videos.Single().Status, Is.EqualTo(VideoStatus.Removed));
            Assert.That(notification.UserId, Is.EqualTo(author.Id));
            Assert.That(notification.Type, Is.EqualTo(NotificationType.VideoRemoved));
            Assert.That(notification.Message, Does.Contain("Breaks the rules"));
        });
    }

    [Test]
    public async Task Taking_down_a_video_twice_is_a_conflict()
    {
        var category = AddCategory();
        var author = AddUser("author");
        var admin = AddUser("admin");
        var video = AddVideo(author.Id, category.Id, status: VideoStatus.Removed);

        var result = await _service.RemoveByModeratorAsync(video.Id, admin.Id, "Again");

        Assert.That(result.ErrorKind, Is.EqualTo(ServiceErrorKind.Conflict));
    }

    // --- Feeds ------------------------------------------------------------

    [Test]
    public async Task The_feed_is_newest_first_and_paged()
    {
        var category = AddCategory();
        var author = AddUser("author");

        for (var i = 0; i < 5; i++)
        {
            var video = AddVideo(author.Id, category.Id, title: $"Video {i}");
            video.CreatedAt = DateTime.UtcNow.AddMinutes(i);
            Db.SaveChanges();
        }

        var firstPage = await _service.GetFeedAsync(null, new PageRequest(1, 2));

        Assert.Multiple(() =>
        {
            Assert.That(firstPage.TotalCount, Is.EqualTo(5));
            Assert.That(firstPage.TotalPages, Is.EqualTo(3));
            Assert.That(firstPage.Items, Has.Count.EqualTo(2));
            Assert.That(firstPage.Items[0].Title, Is.EqualTo("Video 4"));
            Assert.That(firstPage.HasNext, Is.True);
            Assert.That(firstPage.HasPrevious, Is.False);
        });
    }

    [Test]
    public async Task The_following_feed_holds_only_the_authors_the_viewer_follows()
    {
        var category = AddCategory();
        var followed = AddUser("followed");
        var other = AddUser("other");
        var viewer = AddUser("viewer");
        AddFollow(viewer.Id, followed.Id);

        var wanted = AddVideo(followed.Id, category.Id, title: "From the followed author");
        AddVideo(other.Id, category.Id, title: "From somebody else");

        var feed = await _service.GetFollowingFeedAsync(viewer.Id, new PageRequest());

        Assert.That(feed.Items.Select(v => v.Id), Is.EquivalentTo(new[] { wanted.Id }));
    }

    [Test]
    public async Task Search_by_category_and_tag_stays_inside_what_the_viewer_may_see()
    {
        var comedy = AddCategory("Comedy");
        var music = AddCategory("Music");
        var author = AddUser("author");
        var stranger = AddUser("stranger");

        await _service.UploadAsync(BuildUpload(author.Id, comedy.Id, hashtags: "skate"));
        await _service.UploadAsync(BuildUpload(author.Id, music.Id, hashtags: "skate"));
        await _service.UploadAsync(
            BuildUpload(author.Id, comedy.Id, visibility: VideoVisibility.Private, hashtags: "skate"));

        var byCategory = await _service.SearchAsync(
            null, comedy.Id, null, stranger.Id, new PageRequest());

        var byTag = await _service.SearchAsync(
            null, null, "#SKATE", stranger.Id, new PageRequest());

        Assert.Multiple(() =>
        {
            Assert.That(byCategory.TotalCount, Is.EqualTo(1), "the private one must not show up");
            Assert.That(byTag.TotalCount, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task Details_of_a_video_the_viewer_may_not_see_is_NotFound()
    {
        var category = AddCategory();
        var author = AddUser("author");
        var stranger = AddUser("stranger");
        var video = AddVideo(author.Id, category.Id, VideoVisibility.Private);

        var result = await _service.GetDetailsAsync(video.Id, stranger.Id);

        Assert.That(result.ErrorKind, Is.EqualTo(ServiceErrorKind.NotFound));
    }

    [Test]
    public async Task Details_tell_the_author_they_may_delete_it()
    {
        var category = AddCategory();
        var author = AddUser("author");
        var video = AddVideo(author.Id, category.Id);

        var result = await _service.GetDetailsAsync(video.Id, author.Id);

        Assert.That(result.Value!.ViewerCanDelete, Is.True);
    }
}
