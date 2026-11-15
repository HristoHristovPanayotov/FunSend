namespace FunSend.Data.Entities;

/// <summary>Join entity for the many-to-many relation between videos and tags.</summary>
public class VideoHashtag
{
    public int VideoId { get; set; }

    public Video? Video { get; set; }

    public int HashtagId { get; set; }

    public Hashtag? Hashtag { get; set; }
}
