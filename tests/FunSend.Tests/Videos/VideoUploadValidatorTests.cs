using FunSend.Services.Common;
using FunSend.Services.Videos;

namespace FunSend.Tests.Videos;

/// <summary>The format, size and length checks from the risk table.</summary>
[TestFixture]
public class VideoUploadValidatorTests
{
    private static readonly VideoUploadLimits Limits = new()
    {
        MaxFileSizeBytes = 1000,
        MaxDurationSeconds = 180,
        AllowedExtensions = [".mp4", ".webm", ".mov"],
        AllowedContentTypes = ["video/mp4", "video/webm", "video/quicktime"]
    };

    private readonly IVideoUploadValidator _validator =
        new VideoUploadValidator(Microsoft.Extensions.Options.Options.Create(Limits));

    [Test]
    public void Accepts_a_file_inside_every_limit()
    {
        var result = _validator.Validate(new VideoUploadDescriptor("clip.mp4", "video/mp4", 900, 30));

        Assert.That(result.Succeeded, Is.True, result.FirstError);
    }

    [TestCase("clip.avi")]
    [TestCase("clip.exe")]
    [TestCase("clip")]
    public void Refuses_a_format_that_is_not_allowed(string fileName)
    {
        var result = _validator.Validate(new VideoUploadDescriptor(fileName, null, 900, 30));

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorKind, Is.EqualTo(ServiceErrorKind.Validation));
        });
    }

    [Test]
    public void Refuses_a_content_type_that_does_not_match_the_list()
    {
        var result = _validator.Validate(
            new VideoUploadDescriptor("clip.mp4", "application/x-msdownload", 900, 30));

        Assert.That(result.Succeeded, Is.False);
    }

    [Test]
    public void Refuses_a_file_over_the_size_limit()
    {
        var result = _validator.Validate(new VideoUploadDescriptor("clip.mp4", "video/mp4", 1001, 30));

        Assert.That(result.Succeeded, Is.False);
    }

    [Test]
    public void Refuses_an_empty_file()
    {
        var result = _validator.Validate(new VideoUploadDescriptor("clip.mp4", "video/mp4", 0, 30));

        Assert.That(result.Succeeded, Is.False);
    }

    [Test]
    public void Refuses_a_video_longer_than_the_limit()
    {
        var result = _validator.Validate(new VideoUploadDescriptor("clip.mp4", "video/mp4", 900, 181));

        Assert.That(result.Succeeded, Is.False);
    }

    [Test]
    public void Refuses_an_unreadable_length()
    {
        var result = _validator.Validate(new VideoUploadDescriptor("clip.mp4", "video/mp4", 900, 0));

        Assert.That(result.Succeeded, Is.False);
    }

    [Test]
    public void Reports_every_problem_at_once()
    {
        var result = _validator.Validate(new VideoUploadDescriptor("clip.avi", "video/avi", 5000, 900));

        Assert.That(result.Errors, Has.Count.EqualTo(4));
    }

    [TestCase("clip.MP4", ".mp4")]
    [TestCase("holiday.MOV", ".mov")]
    [TestCase("no-extension", "")]
    public void Normalises_the_extension_to_lower_case(string fileName, string expected)
        => Assert.That(_validator.GetNormalisedExtension(fileName), Is.EqualTo(expected));

    [Test]
    public void Accepts_an_upper_case_extension()
    {
        var result = _validator.Validate(new VideoUploadDescriptor("CLIP.MP4", "video/mp4", 900, 30));

        Assert.That(result.Succeeded, Is.True, result.FirstError);
    }
}
