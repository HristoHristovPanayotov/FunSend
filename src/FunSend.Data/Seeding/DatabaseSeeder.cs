using FunSend.Data.Constants;
using FunSend.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FunSend.Data.Seeding;

/// <summary>
/// Fills an empty database with the data the application cannot work without:
/// the three roles, the video categories and, for the project demo, one
/// account per role.
///
/// Every step checks before it writes, so running the seeder again on an
/// existing database changes nothing.
/// </summary>
public class DatabaseSeeder
{
    /// <summary>Categories offered in the upload form.</summary>
    public static readonly string[] DefaultCategories =
    [
        "Comedy",
        "Music",
        "Sport",
        "Gaming",
        "Food",
        "Travel",
        "Education",
        "Animals"
    ];

    private readonly FunSendDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        FunSendDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<DatabaseSeeder> logger)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task SeedAsync(SeedOptions options, CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync();
        await SeedCategoriesAsync(cancellationToken);

        if (!options.CreateDemoAccounts)
        {
            _logger.LogInformation("Demo accounts are switched off; skipping them.");
            return;
        }

        if (string.IsNullOrWhiteSpace(options.DemoPassword))
        {
            _logger.LogWarning(
                "Demo accounts are switched on but no password is configured; skipping them. " +
                "Set {Section}:{Key} to create them.",
                SeedOptions.SectionName,
                nameof(SeedOptions.DemoPassword));
            return;
        }

        await SeedDemoAccountsAsync(options.DemoPassword);
    }

    private async Task SeedRolesAsync()
    {
        foreach (var role in RoleNames.All)
        {
            if (await _roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(role));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not create role {role}: {Describe(result)}");
            }

            _logger.LogInformation("Created role {Role}.", role);
        }
    }

    private async Task SeedCategoriesAsync(CancellationToken cancellationToken)
    {
        var existing = await _db.Categories
            .Select(c => c.Name)
            .ToListAsync(cancellationToken);

        var missing = DefaultCategories
            .Where(name => !existing.Contains(name))
            .Select(name => new Category { Name = name })
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        _db.Categories.AddRange(missing);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created {Count} categories.", missing.Count);
    }

    private async Task SeedDemoAccountsAsync(string password)
    {
        var accounts = new (string Email, string DisplayName, string Role)[]
        {
            ("viewer@funsend.local", "Demo Viewer", RoleNames.Viewer),
            ("creator@funsend.local", "Demo Creator", RoleNames.Creator),
            ("admin@funsend.local", "Demo Admin", RoleNames.Admin)
        };

        foreach (var (email, displayName, role) in accounts)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    DisplayName = displayName,
                    CreatedAt = DateTime.UtcNow
                };

                var created = await _userManager.CreateAsync(user, password);
                if (!created.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Could not create demo account {email}: {Describe(created)}");
                }

                _logger.LogInformation("Created demo account {Email}.", email);
            }

            if (!await _userManager.IsInRoleAsync(user, role))
            {
                var assigned = await _userManager.AddToRoleAsync(user, role);
                if (!assigned.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Could not put {email} in role {role}: {Describe(assigned)}");
                }
            }

            // A creator can do everything a viewer can, so it holds both roles.
            if (role == RoleNames.Creator && !await _userManager.IsInRoleAsync(user, RoleNames.Viewer))
            {
                await _userManager.AddToRoleAsync(user, RoleNames.Viewer);
            }
        }
    }

    private static string Describe(IdentityResult result)
        => string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
}
