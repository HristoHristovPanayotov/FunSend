using FunSend.Data.Entities;
using FunSend.Data.Enums;
using FunSend.Services.Common;
using FunSend.Services.Statistics;

namespace FunSend.Tests.Statistics;

[TestFixture]
public class StatisticsServiceTests : DatabaseTestBase
{
    private StatisticsService _service = null!;

    [SetUp]
    public void SetUpService() => _service = new StatisticsService(Db);

    [Test]
    public async Task The_platform_totals_count_what_is_in_the_database()
    {
        var category = AddCategory();
        var author = AddUser("author");
        var viewer = AddUser("viewer");
        AddUser("blocked", isBlocked: true);

        var video = AddVideo(author.Id, category.Id);
        AddVideo(author.Id, category.Id, status: VideoStatus.Removed);

        Db.Likes.Add(new Like { VideoId = video.Id, UserId = viewer.Id });
        Db.VideoViews.Add(new VideoView { VideoId = video.Id, UserId = viewer.Id });
        Db.VideoViews.Add(new VideoView { VideoId = video.Id, UserId = null });
        Db.Reports.Add(new Report
        {
            VideoId = video.Id,
            ReporterId = viewer.Id,
            Reason = "Spam",
            Status = ReportStatus.Pending
        });
        Db.SaveChanges();

        var stats = await _service.GetPlatformStatisticsAsync();

        Assert.Multiple(() =>
        {
            Assert.That(stats.UserCount, Is.EqualTo(3));
            Assert.That(stats.BlockedUserCount, Is.EqualTo(1));
            Assert.That(stats.VideoCount, Is.EqualTo(2));
            Assert.That(stats.RemovedVideoCount, Is.EqualTo(1));
            Assert.That(stats.LikeCount, Is.EqualTo(1));
            Assert.That(stats.ViewCount, Is.EqualTo(2));
            Assert.That(stats.ViewsLast7Days, Is.EqualTo(2));
            Assert.That(stats.PendingReportCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Views_older_than_a_week_are_left_out_of_the_weekly_figure()
    {
        var category = AddCategory();
        var author = AddUser("author");
        var video = AddVideo(author.Id, category.Id);

        Db.VideoViews.Add(new VideoView { VideoId = video.Id, ViewedAt = DateTime.UtcNow });
        Db.VideoViews.Add(new VideoView
        {
            VideoId = video.Id,
            ViewedAt = DateTime.UtcNow.AddDays(-30)
        });
        Db.SaveChanges();

        var stats = await _service.GetPlatformStatisticsAsync();

        Assert.Multiple(() =>
        {
            Assert.That(stats.ViewCount, Is.EqualTo(2));
            Assert.That(stats.ViewsLast7Days, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task The_most_watched_table_is_ordered_by_views()
    {
        var category = AddCategory();
        var author = AddUser("author");
        var quiet = AddVideo(author.Id, category.Id, title: "Quiet");
        var popular = AddVideo(author.Id, category.Id, title: "Popular");

        for (var i = 0; i < 3; i++)
        {
            Db.VideoViews.Add(new VideoView { VideoId = popular.Id });
        }

        Db.VideoViews.Add(new VideoView { VideoId = quiet.Id });
        Db.SaveChanges();

        var mostWatched = await _service.GetMostWatchedAsync();

        Assert.Multiple(() =>
        {
            Assert.That(mostWatched[0].Title, Is.EqualTo("Popular"));
            Assert.That(mostWatched[0].Views, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task Only_authors_with_videos_appear_among_the_most_active()
    {
        var category = AddCategory();
        var author = AddUser("author", "The Author");
        AddUser("lurker", "A Lurker");
        AddVideo(author.Id, category.Id);

        var creators = await _service.GetMostActiveCreatorsAsync();

        Assert.Multiple(() =>
        {
            Assert.That(creators, Has.Count.EqualTo(1));
            Assert.That(creators[0].DisplayName, Is.EqualTo("The Author"));
            Assert.That(creators[0].Videos, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Views_per_day_covers_the_whole_window_and_fills_quiet_days_with_zero()
    {
        var category = AddCategory();
        var author = AddUser("author");
        var video = AddVideo(author.Id, category.Id);

        Db.VideoViews.Add(new VideoView { VideoId = video.Id, ViewedAt = DateTime.UtcNow });
        Db.SaveChanges();

        var days = await _service.GetViewsPerDayAsync(7);

        Assert.Multiple(() =>
        {
            Assert.That(days, Has.Count.EqualTo(7));
            Assert.That(days.Sum(d => d.Views), Is.EqualTo(1));
            Assert.That(days.Count(d => d.Views == 0), Is.EqualTo(6));
        });
    }

    [Test]
    public async Task An_author_can_read_their_own_statistics()
    {
        var category = AddCategory();
        var author = AddUser("author");
        AddVideo(author.Id, category.Id);

        var result = await _service.GetCreatorStatisticsAsync(author.Id, author.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True, result.FirstError);
            Assert.That(result.Value!.Videos, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Reading_somebody_elses_statistics_is_Forbidden()
    {
        var author = AddUser("author");
        var stranger = AddUser("stranger");

        var result = await _service.GetCreatorStatisticsAsync(author.Id, stranger.Id);

        Assert.That(result.ErrorKind, Is.EqualTo(ServiceErrorKind.Forbidden));
    }

    [Test]
    public async Task An_administrator_may_read_anybody_statistics()
    {
        var category = AddCategory();
        var author = AddUser("author");
        var admin = AddUser("admin");
        AddVideo(author.Id, category.Id);

        var result = await _service.GetCreatorStatisticsAsync(author.Id, admin.Id, isAdmin: true);

        Assert.That(result.Succeeded, Is.True, result.FirstError);
    }
}
