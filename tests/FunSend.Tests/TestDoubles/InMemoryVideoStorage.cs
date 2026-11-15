using FunSend.Services.Storage;

namespace FunSend.Tests.TestDoubles;

/// <summary>
/// Storage that keeps the bytes in a dictionary. It exists because
/// <see cref="IVideoStorage"/> is an interface: the upload rules can be tested
/// without touching the disk, which is the point of the Strategy in the design.
/// </summary>
public class InMemoryVideoStorage : IVideoStorage
{
    private readonly Dictionary<string, byte[]> _files = [];

    public IReadOnlyDictionary<string, byte[]> Files => _files;

    public int SaveCount { get; private set; }

    public int DeleteCount { get; private set; }

    /// <summary>Set to make the next save throw, to test the clean-up path.</summary>
    public Exception? ThrowOnSave { get; set; }

    public async Task<string> SaveAsync(
        Stream content,
        string extension,
        CancellationToken cancellationToken = default)
    {
        SaveCount++;

        if (ThrowOnSave is not null)
        {
            throw ThrowOnSave;
        }

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);

        var name = $"{Guid.NewGuid():N}{extension}";
        _files[name] = buffer.ToArray();

        return name;
    }

    public Task<Stream?> OpenReadAsync(
        string storedFileName,
        CancellationToken cancellationToken = default)
        => Task.FromResult<Stream?>(_files.TryGetValue(storedFileName, out var bytes)
            ? new MemoryStream(bytes)
            : null);

    public Task DeleteAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        DeleteCount++;
        _files.Remove(storedFileName);

        return Task.CompletedTask;
    }

    public bool Exists(string storedFileName) => _files.ContainsKey(storedFileName);
}
