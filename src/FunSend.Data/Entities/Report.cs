using FunSend.Data.Enums;

namespace FunSend.Data.Entities;

/// <summary>
/// An abuse report. Exactly one of <see cref="VideoId"/> and
/// <see cref="CommentId"/> is set, which a check constraint enforces.
/// </summary>
public class Report
{
    public const int ReasonMaxLength = 400;

    public int Id { get; set; }

    public string Reason { get; set; } = string.Empty;

    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedAt { get; set; }

    public int? VideoId { get; set; }

    public Video? Video { get; set; }

    public int? CommentId { get; set; }

    public Comment? Comment { get; set; }

    public string ReporterId { get; set; } = string.Empty;

    public ApplicationUser? Reporter { get; set; }
}
