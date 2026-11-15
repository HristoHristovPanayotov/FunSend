using FunSend.Data.Enums;
using FunSend.Services.Common;
using FunSend.Services.Notifications;
using FunSend.Services.Videos;

namespace FunSend.Tests.Notifications;

[TestFixture]
public class NotificationServiceTests : DatabaseTestBase
{
    private NotificationService _service = null!;

    [SetUp]
    public void SetUpService() => _service = new NotificationService(Db);

    [Test]
    public async Task A_notification_is_stored_unread()
    {
        var recipient = AddUser("recipient");
        var actor = AddUser("actor");

        var result = await _service.NotifyAsync(
            recipient.Id,
            NotificationType.NewFollower,
            "Somebody started following you.",
            actor.Id);

        var notification = Db.Notifications.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True, result.FirstError);
            Assert.That(notification.IsRead, Is.False);
            Assert.That(notification.ActorId, Is.EqualTo(actor.Id));
        });
    }

    [Test]
    public async Task Nobody_is_told_about_their_own_action()
    {
        var user = AddUser("user");

        var result = await _service.NotifyAsync(
            user.Id,
            NotificationType.VideoLiked,
            "You liked your own video.",
            user.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True, "this is not an error, it is simply not sent");
            Assert.That(Db.Notifications.Count(), Is.Zero);
        });
    }

    [Test]
    public async Task A_recipient_who_does_not_exist_is_NotFound()
    {
        var result = await _service.NotifyAsync(
            "ghost",
            NotificationType.NewFollower,
            "Hello");

        Assert.That(result.ErrorKind, Is.EqualTo(ServiceErrorKind.NotFound));
    }

    [Test]
    public async Task An_empty_message_is_refused()
    {
        var user = AddUser("user");

        var result = await _service.NotifyAsync(user.Id, NotificationType.NewFollower, "  ");

        Assert.That(result.ErrorKind, Is.EqualTo(ServiceErrorKind.Validation));
    }

    [Test]
    public async Task A_message_longer_than_the_column_is_cut_to_fit()
    {
        var user = AddUser("user");

        await _service.NotifyAsync(
            user.Id,
            NotificationType.VideoRemoved,
            new string('x', 500));

        Assert.That(
            Db.Notifications.Single().Message,
            Has.Length.EqualTo(Data.Entities.Notification.MessageMaxLength));
    }

    [Test]
    public async Task The_unread_count_follows_what_was_read()
    {
        var user = AddUser("user");
        var actor = AddUser("actor");

        await _service.NotifyAsync(user.Id, NotificationType.NewFollower, "One", actor.Id);
        await _service.NotifyAsync(user.Id, NotificationType.NewFollower, "Two", actor.Id);

        var before = await _service.GetUnreadCountAsync(user.Id);

        var first = Db.Notifications.First();
        await _service.MarkAsReadAsync(first.Id, user.Id);

        var after = await _service.GetUnreadCountAsync(user.Id);

        Assert.Multiple(() =>
        {
            Assert.That(before, Is.EqualTo(2));
            Assert.That(after, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Marking_somebody_elses_notification_read_is_Forbidden()
    {
        var owner = AddUser("owner");
        var other = AddUser("other");
        var actor = AddUser("actor");

        await _service.NotifyAsync(owner.Id, NotificationType.NewFollower, "One", actor.Id);
        var notification = Db.Notifications.Single();

        var result = await _service.MarkAsReadAsync(notification.Id, other.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorKind, Is.EqualTo(ServiceErrorKind.Forbidden));
            Assert.That(Db.Notifications.Single().IsRead, Is.False);
        });
    }

    [Test]
    public async Task Mark_all_as_read_leaves_nothing_unread()
    {
        var user = AddUser("user");
        var actor = AddUser("actor");

        await _service.NotifyAsync(user.Id, NotificationType.NewFollower, "One", actor.Id);
        await _service.NotifyAsync(user.Id, NotificationType.VideoLiked, "Two", actor.Id);

        await _service.MarkAllAsReadAsync(user.Id);

        Assert.That(await _service.GetUnreadCountAsync(user.Id), Is.Zero);
    }

    [Test]
    public async Task The_list_can_be_narrowed_to_the_unread_ones()
    {
        var user = AddUser("user");
        var actor = AddUser("actor");

        await _service.NotifyAsync(user.Id, NotificationType.NewFollower, "One", actor.Id);
        await _service.NotifyAsync(user.Id, NotificationType.VideoLiked, "Two", actor.Id);
        await _service.MarkAsReadAsync(Db.Notifications.First().Id, user.Id);

        var unread = await _service.GetForUserAsync(user.Id, new PageRequest(), unreadOnly: true);

        Assert.That(unread.TotalCount, Is.EqualTo(1));
    }
}
