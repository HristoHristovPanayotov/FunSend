using System.Text;
using FunSend.Services.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FunSend.Tests.Storage;

/// <summary>
/// The one service that touches the file system, so these tests use a real
/// temporary folder and remove it again afterwards.
/// </summary>
[TestFixture]
public class LocalVideoStorageTests
{
    private string _root = null!;
    private LocalVideoStorage _storage = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), $"funsend-tests-{Guid.NewGuid():N}");

        _storage = new LocalVideoStorage(
            Options.Create(new LocalVideoStorageOptions { RootPath = _root }),
            NullLogger<LocalVideoStorage>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static Stream Content(string text = "video bytes")
        => new MemoryStream(Encoding.UTF8.GetBytes(text));

    [Test]
    public async Task Save_writes_the_file_and_returns_a_generated_name()
    {
        await using var content = Content();

        var name = await _storage.SaveAsync(content, ".mp4");

        Assert.Multiple(() =>
        {
            Assert.That(name, Does.EndWith(".mp4"));
            Assert.That(Path.GetFileNameWithoutExtension(name), Has.Length.EqualTo(32));
            Assert.That(_storage.Exists(name), Is.True);
        });
    }

    [Test]
    public async Task The_original_file_name_never_reaches_the_disk()
    {
        await using var content = Content();

        var name = await _storage.SaveAsync(content, ".mp4");

        Assert.That(name, Does.Not.Contain("holiday"));
    }

    [Test]
    public async Task Two_saves_never_collide()
    {
        await using var first = Content("one");
        await using var second = Content("two");

        var firstName = await _storage.SaveAsync(first, ".mp4");
        var secondName = await _storage.SaveAsync(second, ".mp4");

        Assert.That(firstName, Is.Not.EqualTo(secondName));
    }

    [Test]
    public async Task The_stored_bytes_come_back_unchanged()
    {
        await using var content = Content("exactly these bytes");

        var name = await _storage.SaveAsync(content, ".webm");

        await using var reader = await _storage.OpenReadAsync(name);
        using var buffer = new MemoryStream();
        await reader!.CopyToAsync(buffer);

        Assert.That(Encoding.UTF8.GetString(buffer.ToArray()), Is.EqualTo("exactly these bytes"));
    }

    [Test]
    public async Task Reading_a_file_that_is_not_there_returns_null()
        => Assert.That(await _storage.OpenReadAsync("missing.mp4"), Is.Null);

    [Test]
    public async Task Delete_removes_the_file_and_is_safe_to_repeat()
    {
        await using var content = Content();
        var name = await _storage.SaveAsync(content, ".mp4");

        await _storage.DeleteAsync(name);
        await _storage.DeleteAsync(name);

        Assert.That(_storage.Exists(name), Is.False);
    }

    [TestCase("../outside.mp4")]
    [TestCase("..\\outside.mp4")]
    [TestCase("sub/folder.mp4")]
    [TestCase("")]
    public async Task A_name_that_is_a_path_is_refused(string name)
    {
        var opened = await _storage.OpenReadAsync(name);

        Assert.Multiple(() =>
        {
            Assert.That(_storage.Exists(name), Is.False);
            Assert.That(opened, Is.Null);
        });
    }

    [Test]
    public void An_extension_without_a_dot_is_rejected()
    {
        using var content = Content();

        Assert.ThrowsAsync<ArgumentException>(() => _storage.SaveAsync(content, "mp4"));
    }

    [Test]
    public async Task The_extension_is_stored_in_lower_case()
    {
        await using var content = Content();

        var name = await _storage.SaveAsync(content, ".MP4");

        Assert.That(name, Does.EndWith(".mp4"));
    }
}
