using FunSend.Services.Common;
using Microsoft.Extensions.Options;

namespace FunSend.Services.Videos;

/// <summary>What the validator is given about an upload, with no ASP.NET types.</summary>
/// <param name="FileName">Original file name, used only for its extension.</param>
/// <param name="ContentType">Content type the browser sent, when there is one.</param>
/// <param name="SizeBytes">Size of the uploaded file.</param>
/// <param name="DurationSeconds">Length of the video as the client reported it.</param>
public sealed record VideoUploadDescriptor(
    string FileName,
    string? ContentType,
    long SizeBytes,
    int DurationSeconds);

/// <summary>Checks format, size and length before anything is written.</summary>
public interface IVideoUploadValidator
{
    ServiceResult Validate(VideoUploadDescriptor upload);

    /// <summary>The extension to store the file under, lower case and with the dot.</summary>
    string GetNormalisedExtension(string fileName);
}

/// <inheritdoc cref="IVideoUploadValidator"/>
public class VideoUploadValidator : IVideoUploadValidator
{
    private readonly VideoUploadLimits _limits;

    public VideoUploadValidator(IOptions<VideoUploadLimits> limits)
    {
        _limits = limits.Value;
    }

    public ServiceResult Validate(VideoUploadDescriptor upload)
    {
        ArgumentNullException.ThrowIfNull(upload);

        var errors = new List<string>();

        var extension = GetNormalisedExtension(upload.FileName);

        if (extension.Length == 0)
        {
            errors.Add("The file has no extension.");
        }
        else if (!_limits.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"The format {extension} is not allowed. Allowed: {string.Join(", ", _limits.AllowedExtensions)}.");
        }

        if (!string.IsNullOrWhiteSpace(upload.ContentType)
            && !_limits.AllowedContentTypes.Contains(upload.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"The content type {upload.ContentType} is not allowed.");
        }

        if (upload.SizeBytes <= 0)
        {
            errors.Add("The file is empty.");
        }
        else if (upload.SizeBytes > _limits.MaxFileSizeBytes)
        {
            errors.Add($"The file is larger than {_limits.MaxFileSizeMegabytes:0} MB.");
        }

        if (upload.DurationSeconds <= 0)
        {
            errors.Add("The length of the video could not be read.");
        }
        else if (upload.DurationSeconds > _limits.MaxDurationSeconds)
        {
            errors.Add($"The video is longer than {_limits.MaxDurationSeconds} seconds.");
        }

        return errors.Count == 0
            ? ServiceResult.Success()
            : ServiceResult.Invalid([.. errors]);
    }

    public string GetNormalisedExtension(string fileName)
        => string.IsNullOrWhiteSpace(fileName)
            ? string.Empty
            : Path.GetExtension(fileName).ToLowerInvariant();
}
