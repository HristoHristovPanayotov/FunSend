namespace FunSend.Data.Entities;

/// <summary>A fixed list of video categories, filled in by seeding.</summary>
public class Category
{
    public const int NameMaxLength = 50;

    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Video> Videos { get; set; } = new List<Video>();
}
