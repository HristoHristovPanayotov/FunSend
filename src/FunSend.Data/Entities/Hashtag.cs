namespace FunSend.Data.Entities;

/// <summary>A tag, stored once and shared by every video that uses it.</summary>
public class Hashtag
{
    public const int NameMaxLength = 40;

    public int Id { get; set; }

    /// <summary>Lower-case name, stored without the leading hash sign.</summary>
    public string Name { get; set; } = string.Empty;

    public ICollection<VideoHashtag> VideoHashtags { get; set; } = new List<VideoHashtag>();
}
