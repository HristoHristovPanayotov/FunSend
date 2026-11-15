namespace FunSend.Data.Enums;

/// <summary>Moderation state of a video.</summary>
public enum VideoStatus
{
    /// <summary>Normal state after a successful upload.</summary>
    Published = 0,

    /// <summary>Taken down by a moderator. The row is kept for the record.</summary>
    Removed = 1
}
