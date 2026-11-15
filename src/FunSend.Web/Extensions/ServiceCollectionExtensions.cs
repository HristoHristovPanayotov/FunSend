using FunSend.Data;
using FunSend.Data.Entities;
using FunSend.Data.Seeding;
using FunSend.Services.Notifications;
using FunSend.Services.Social;
using FunSend.Services.Statistics;
using FunSend.Services.Storage;
using FunSend.Services.Videos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FunSend.Web.Extensions;

/// <summary>
/// Registration of the FunSend layers. Everything is injected through its
/// interface, which is the second design pattern of the project: the services
/// can be replaced by test doubles without touching the controllers.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFunSendData(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "The connection string DefaultConnection is not configured.");

        services.AddDbContext<FunSendDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        services.AddDatabaseDeveloperPageExceptionFilter();

        return services;
    }

    public static IServiceCollection AddFunSendIdentity(this IServiceCollection services)
    {
        services
            .AddDefaultIdentity<ApplicationUser>(options =>
            {
                // Sign-in
                options.SignIn.RequireConfirmedAccount = false;

                // Passwords
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;

                // Lockout, which is also how a blocked user is kept out
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

                // Users
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<FunSendDbContext>();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "FunSend.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.LoginPath = "/Identity/Account/Login";
            options.LogoutPath = "/Identity/Account/Logout";
            options.AccessDeniedPath = "/error/403";
        });

        // Blocking a user stamps their account, so the cookie stops working
        // within this interval instead of at its own expiry.
        services.Configure<SecurityStampValidatorOptions>(options =>
            options.ValidationInterval = TimeSpan.FromMinutes(5));

        return services;
    }

    public static IServiceCollection AddFunSendServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRootPath)
    {
        services.Configure<VideoUploadLimits>(
            configuration.GetSection(VideoUploadLimits.SectionName));

        services.Configure<SeedOptions>(
            configuration.GetSection(SeedOptions.SectionName));

        services.Configure<LocalVideoStorageOptions>(
            configuration.GetSection(LocalVideoStorageOptions.SectionName));

        // A relative storage path is meant relative to the application, not to
        // whatever folder the process happens to be started from.
        services.PostConfigure<LocalVideoStorageOptions>(options =>
        {
            if (!Path.IsPathRooted(options.RootPath))
            {
                options.RootPath = Path.Combine(contentRootPath, options.RootPath);
            }
        });

        services.AddScoped<IVideoStorage, LocalVideoStorage>();
        services.AddScoped<IVideoUploadValidator, VideoUploadValidator>();
        services.AddScoped<IVideoService, VideoService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ISocialService, SocialService>();
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
