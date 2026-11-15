using FunSend.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FunSend.Data;

/// <summary>
/// Code First context for FunSend. Identity brings the user, role and claim
/// tables; everything else is configured in <see cref="OnModelCreating"/>.
///
/// Every relation that points at a user uses DeleteBehavior.Restrict, because
/// SQL Server refuses a schema with more than one cascade path into the same
/// table and a user is reachable from almost every table here.
/// </summary>
public class FunSendDbContext : IdentityDbContext<ApplicationUser>
{
    public FunSendDbContext(DbContextOptions<FunSendDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Video> Videos => Set<Video>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<SavedVideo> SavedVideos => Set<SavedVideo>();
    public DbSet<Follow> Follows => Set<Follow>();
    public DbSet<Hashtag> Hashtags => Set<Hashtag>();
    public DbSet<VideoHashtag> VideoHashtags => Set<VideoHashtag>();
    public DbSet<VideoView> VideoViews => Set<VideoView>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Report> Reports => Set<Report>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureUsers(builder);
        ConfigureCategories(builder);
        ConfigureVideos(builder);
        ConfigureComments(builder);
        ConfigureLikes(builder);
        ConfigureSavedVideos(builder);
        ConfigureFollows(builder);
        ConfigureHashtags(builder);
        ConfigureVideoViews(builder);
        ConfigureNotifications(builder);
        ConfigureReports(builder);
    }

    private static void ConfigureUsers(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.DisplayName)
                .IsRequired()
                .HasMaxLength(ApplicationUser.DisplayNameMaxLength);

            entity.Property(u => u.Bio)
                .HasMaxLength(ApplicationUser.BioMaxLength);

            entity.Property(u => u.AvatarFileName)
                .HasMaxLength(Video.StoredFileNameMaxLength);

            entity.Property(u => u.CreatedAt)
                .IsRequired();

            entity.HasIndex(u => u.DisplayName);
        });
    }

    private static void ConfigureCategories(ModelBuilder builder)
    {
        builder.Entity<Category>(entity =>
        {
            entity.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(Category.NameMaxLength);

            entity.HasIndex(c => c.Name).IsUnique();
        });
    }

    private static void ConfigureVideos(ModelBuilder builder)
    {
        builder.Entity<Video>(entity =>
        {
            entity.Property(v => v.Title)
                .IsRequired()
                .HasMaxLength(Video.TitleMaxLength);

            entity.Property(v => v.Description)
                .HasMaxLength(Video.DescriptionMaxLength);

            entity.Property(v => v.StoredFileName)
                .IsRequired()
                .HasMaxLength(Video.StoredFileNameMaxLength);

            entity.Property(v => v.Status)
                .HasConversion<int>();

            entity.Property(v => v.Visibility)
                .HasConversion<int>();

            entity.HasOne(v => v.User)
                .WithMany(u => u.Videos)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(v => v.Category)
                .WithMany(c => c.Videos)
                .HasForeignKey(v => v.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(v => v.CreatedAt);
            entity.HasIndex(v => new { v.UserId, v.CreatedAt });
            entity.HasIndex(v => new { v.Status, v.Visibility, v.CreatedAt });

            entity.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_Videos_DurationSeconds_Positive",
                    "[DurationSeconds] > 0");

                t.HasCheckConstraint(
                    "CK_Videos_FileSizeBytes_Positive",
                    "[FileSizeBytes] > 0");
            });
        });
    }

    private static void ConfigureComments(ModelBuilder builder)
    {
        builder.Entity<Comment>(entity =>
        {
            entity.Property(c => c.Text)
                .IsRequired()
                .HasMaxLength(Comment.TextMaxLength);

            entity.HasOne(c => c.Video)
                .WithMany(v => v.Comments)
                .HasForeignKey(c => c.VideoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(c => new { c.VideoId, c.CreatedAt });
        });
    }

    private static void ConfigureLikes(ModelBuilder builder)
    {
        builder.Entity<Like>(entity =>
        {
            entity.HasKey(l => new { l.VideoId, l.UserId });

            entity.HasOne(l => l.Video)
                .WithMany(v => v.Likes)
                .HasForeignKey(l => l.VideoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(l => l.User)
                .WithMany(u => u.Likes)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(l => l.UserId);
        });
    }

    private static void ConfigureSavedVideos(ModelBuilder builder)
    {
        builder.Entity<SavedVideo>(entity =>
        {
            entity.HasKey(s => new { s.VideoId, s.UserId });

            entity.HasOne(s => s.Video)
                .WithMany(v => v.SavedBy)
                .HasForeignKey(s => s.VideoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.User)
                .WithMany(u => u.SavedVideos)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(s => new { s.UserId, s.CreatedAt });
        });
    }

    private static void ConfigureFollows(ModelBuilder builder)
    {
        builder.Entity<Follow>(entity =>
        {
            entity.HasKey(f => new { f.FollowerId, f.FollowingId });

            entity.HasOne(f => f.Follower)
                .WithMany(u => u.Following)
                .HasForeignKey(f => f.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(f => f.Following)
                .WithMany(u => u.Followers)
                .HasForeignKey(f => f.FollowingId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(f => f.FollowingId);

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Follows_NoSelfFollow",
                "[FollowerId] <> [FollowingId]"));
        });
    }

    private static void ConfigureHashtags(ModelBuilder builder)
    {
        builder.Entity<Hashtag>(entity =>
        {
            entity.Property(h => h.Name)
                .IsRequired()
                .HasMaxLength(Hashtag.NameMaxLength);

            entity.HasIndex(h => h.Name).IsUnique();
        });

        builder.Entity<VideoHashtag>(entity =>
        {
            entity.HasKey(vh => new { vh.VideoId, vh.HashtagId });

            entity.HasOne(vh => vh.Video)
                .WithMany(v => v.VideoHashtags)
                .HasForeignKey(vh => vh.VideoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(vh => vh.Hashtag)
                .WithMany(h => h.VideoHashtags)
                .HasForeignKey(vh => vh.HashtagId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(vh => vh.HashtagId);
        });
    }

    private static void ConfigureVideoViews(ModelBuilder builder)
    {
        builder.Entity<VideoView>(entity =>
        {
            entity.HasOne(v => v.Video)
                .WithMany(v => v.Views)
                .HasForeignKey(v => v.VideoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(v => new { v.VideoId, v.ViewedAt });
        });
    }

    private static void ConfigureNotifications(ModelBuilder builder)
    {
        builder.Entity<Notification>(entity =>
        {
            entity.Property(n => n.Message)
                .IsRequired()
                .HasMaxLength(Notification.MessageMaxLength);

            entity.Property(n => n.Type)
                .HasConversion<int>();

            entity.HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(n => n.Actor)
                .WithMany()
                .HasForeignKey(n => n.ActorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(n => n.Video)
                .WithMany()
                .HasForeignKey(n => n.VideoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });
        });
    }

    private static void ConfigureReports(ModelBuilder builder)
    {
        builder.Entity<Report>(entity =>
        {
            entity.Property(r => r.Reason)
                .IsRequired()
                .HasMaxLength(Report.ReasonMaxLength);

            entity.Property(r => r.Status)
                .HasConversion<int>();

            entity.HasOne(r => r.Video)
                .WithMany()
                .HasForeignKey(r => r.VideoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Comment)
                .WithMany()
                .HasForeignKey(r => r.CommentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Reporter)
                .WithMany()
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(r => new { r.Status, r.CreatedAt });

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Reports_ExactlyOneTarget",
                "([VideoId] IS NOT NULL AND [CommentId] IS NULL) OR ([VideoId] IS NULL AND [CommentId] IS NOT NULL)"));
        });
    }
}
