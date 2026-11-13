using FunSend.Data;
using FunSend.Data.Seeding;
using FunSend.Services.Videos;
using FunSend.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFunSendData(builder.Configuration);
builder.Services.AddFunSendIdentity();
builder.Services.AddFunSendServices(builder.Configuration, builder.Environment.ContentRootPath);

builder.Services.AddControllersWithViews(options =>
{
    // Every POST is checked for an antiforgery token, so no action can forget it.
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// Identity brings its own Razor Pages for register, login and account management.
builder.Services.AddRazorPages();

// The request body limit follows the configured upload limit, with a little
// room for the rest of the multipart form.
var uploadLimits = builder.Configuration
    .GetSection(VideoUploadLimits.SectionName)
    .Get<VideoUploadLimits>() ?? new VideoUploadLimits();

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = uploadLimits.MaxFileSizeBytes + 1024 * 1024;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = uploadLimits.MaxFileSizeBytes + 1024 * 1024;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/error/500");
    app.UseHsts();
}

// Custom pages for 404, 403 and the other status codes, as the task requires.
app.UseStatusCodePagesWithReExecute("/error/{0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

await ApplyMigrationsAndSeedAsync(app);

app.Run();

// Brings the database up to date and fills in the roles, categories and demo
// accounts. Running this on start-up is what makes the public demo
// reproducible from an empty database.
static async Task ApplyMigrationsAndSeedAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var provider = scope.ServiceProvider;
    var logger = provider.GetRequiredService<ILogger<Program>>();

    try
    {
        var db = provider.GetRequiredService<FunSendDbContext>();
        await db.Database.MigrateAsync();

        var seeder = provider.GetRequiredService<DatabaseSeeder>();
        var options = provider.GetRequiredService<IOptions<SeedOptions>>().Value;
        await seeder.SeedAsync(options);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Preparing the database failed.");
        throw;
    }
}
