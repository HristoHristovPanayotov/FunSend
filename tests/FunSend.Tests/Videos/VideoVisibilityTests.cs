using FunSend.Data.Enums;
using FunSend.Services.Videos;
using Microsoft.EntityFrameworkCore;

namespace FunSend.Tests.Videos;

/// <summary>
/// The visibility rule is the one piece of logic every listing depends on, so
/// it is tested on its own rather than only through the service.
/// </summary>
[TestFixture]
public class VideoVisibilityTests : DatabaseTestBase
{
    [Test]
    public async Task Guest_sees_only_public_published_videos()
    {
        var category = AddCategory();
        var author = AddUser("author");

        var publicVideo = AddVideo(author.Id, category.Id);
        AddVideo(author.Id, category.Id, VideoVisibility.FollowersOnly);
        AddVideo(author.Id, category.Id, VideoVisibility.Private);
        AddVideo(author.Id, category.Id, status: VideoStatus.Removed);

        var visible = await Db.Videos.VisibleTo(Db, viewerId: null).ToListAsync();

        Assert.That(visible.Select(v => v.Id), Is.EquivalentTo(new[] { publicVideo.Id }));
    }

    [Test]
    public async Task Author_sees_every_video_of_their_own()
    {
        var category = AddCategory();
        var author = AddUser("author");

        AddVideo(author.Id, category.Id);
        AddVideo(author.Id, category.Id, VideoVisibility.Private);
        AddVideo(author.Id, category.Id, status: VideoStatus.Removed);

        var visible = await Db.Videos.VisibleTo(Db, author.Id).ToListAsync();

        Assert.That(visible, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task Follower_sees_a_followers_only_video()
    {
        var category = AddCategory();
        var author = AddUser("author");
        var follower = AddUser("follower");
        AddFollow(follower.Id, author.Id);

        var restricted = AddVideo(author.Id, category.Id, VideoVisibility.FollowersOnly);

        var visible = await Db.Videos.VisibleTo(Db, follower.Id).ToListAsync();

        Assert.That(visible.Select(v => v.Id), Does.Contain(restricted.Id));
    }

    [Test]
    public async Task Stranger_does_not_see_a_followers_only_video()
    {
        var category = AddCategory();
        var author = AddUser("author");
        var stranger = AddUser("stranger");

        AddVideo(author.Id, category.Id, VideoVisibility.FollowersOnly);

        var visible = await Db.Videos.VisibleTo(Db, stranger.Id).ToListAsync();

        Assert.That(visible, Is.Empty);
    }

    [Test]
    public async Task Nobody_but_the_author_sees_a_private_video()
    {
        var category = AddCategory();
        var author = AddUser("author");
        var follower = AddUser("follower");
        AddFollow(follower.Id, author.Id);

        AddVideo(author.Id, category.Id, VideoVisibility.Private);

        var visible = await Db.Videos.VisibleTo(Db, follower.Id).ToListAsync();

        Assert.That(visible, Is.Empty);
    }

    [Test]
    public async Task Administrator_sees_everything_including_what_was_taken_down()
    {
        var category = AddCategory();
        var author = AddUser("author");
        var admin = AddUser("admin");

        AddVideo(author.Id, category.Id, VideoVisibility.Private);
        AddVideo(author.Id, category.Id, status: VideoStatus.Removed);

        var visible = await Db.Videos.VisibleTo(Db, admin.Id, isAdmin: true).ToListAsync();

        Assert.That(visible, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task A_video_taken_down_disappears_from_the_feed_of_others()
    {
        var category = AddCategory();
        var author = AddUser("author");
        var viewer = AddUser("viewer");

        AddVideo(author.Id, category.Id, status: VideoStatus.Removed);

        var visible = await Db.Videos.VisibleTo(Db, viewer.Id).ToListAsync();

        Assert.That(visible, Is.Empty);
    }
}
