namespace FunSend.Services.Videos;

/// <summary>
/// The upload limits from the risk table of the design: 100 MB, 180 seconds
/// and three container formats. They live in configuration because the
/// research showed that every platform sets its own maximum length and
/// changes it over time.
/// </summary>
public class VideoUploadLimits
{
    public const string SectionName = "VideoUpload";

    public long MaxFileSizeBytes { get; set; } = 100L * 1024 * 1024;

    public int MaxDurationSeconds { get; set; } = 180;

    public List<string> AllowedExtensions { get; set; } = [".mp4", ".webm", ".mov"];

    public List<string> AllowedContentTypes { get; set; } =
    [
        "video/mp4",
        "video/webm",
        "video/quicktime"
    ];

    public double MaxFileSizeMegabytes => MaxFileSizeBytes / 1024d / 1024d;
}
