using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FunSend.Services.Storage;

/// <summary>Where <see cref="LocalVideoStorage"/> writes and what it allows.</summary>
public class LocalVideoStorageOptions
{
    public const string SectionName = "VideoStorage";

    /// <summary>
    /// Folder the files are written to. A relative path is resolved against the
    /// content root of the application.
    ///
    /// The default is outside wwwroot on purpose: a video is served by the
    /// controller, which checks the visibility rule first, so a private video
    /// cannot be fetched with a direct link to a static file.
    /// </summary>
    public string RootPath { get; set; } = Path.Combine("App_Data", "videos");
}

/// <summary>
/// Keeps video files on the local disk under GUID names. The original file
/// name never reaches the file system, which removes path traversal and
/// collisions in one step, and the database keeps only the generated name.
/// </summary>
public class LocalVideoStorage : IVideoStorage
{
    private readonly string _rootPath;
    private readonly ILogger<LocalVideoStorage> _logger;

    public LocalVideoStorage(IOptions<LocalVideoStorageOptions> options, ILogger<LocalVideoStorage> logger)
    {
        _rootPath = Path.GetFullPath(options.Value.RootPath);
        _logger = logger;
    }

    public async Task<string> SaveAsync(
        Stream content,
        string extension,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (string.IsNullOrWhiteSpace(extension) || !extension.StartsWith('.'))
        {
            throw new ArgumentException("The extension must start with a dot.", nameof(extension));
        }

        Directory.CreateDirectory(_rootPath);

        var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var fullPath = Path.Combine(_rootPath, storedFileName);

        try
        {
            await using var target = new FileStream(
                fullPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            await content.CopyToAsync(target, cancellationToken);
        }
        catch
        {
            // Never leave half a file behind for a row that will not be written.
            TryDelete(fullPath);
            throw;
        }

        _logger.LogInformation("Stored video file {StoredFileName}.", storedFileName);
        return storedFileName;
    }

    public Task<Stream?> OpenReadAsync(
        string storedFileName,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolve(storedFileName, out var fullPath) || !File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        if (TryResolve(storedFileName, out var fullPath))
        {
            TryDelete(fullPath);
        }

        return Task.CompletedTask;
    }

    public bool Exists(string storedFileName)
        => TryResolve(storedFileName, out var fullPath) && File.Exists(fullPath);

    /// <summary>
    /// Turns a stored name into a full path, refusing anything that is not a
    /// bare file name directly inside the storage folder.
    /// </summary>
    private bool TryResolve(string storedFileName, out string fullPath)
    {
        fullPath = string.Empty;

        if (string.IsNullOrWhiteSpace(storedFileName))
        {
            return false;
        }

        if (storedFileName != Path.GetFileName(storedFileName))
        {
            _logger.LogWarning("Refused a stored file name that is a path: {StoredFileName}.", storedFileName);
            return false;
        }

        var candidate = Path.GetFullPath(Path.Combine(_rootPath, storedFileName));

        if (!candidate.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Refused a stored file name outside the storage folder: {StoredFileName}.", storedFileName);
            return false;
        }

        fullPath = candidate;
        return true;
    }

    private void TryDelete(string fullPath)
    {
        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Could not delete {FullPath}.", fullPath);
        }
    }
}
