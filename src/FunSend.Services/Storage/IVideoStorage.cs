namespace FunSend.Services.Storage;

/// <summary>
/// Where video files are kept. This is the Strategy of the design: the rest of
/// the application only knows this interface, so the local-disk implementation
/// used for the project demo can be swapped for cloud storage without touching
/// the services or the controllers.
/// </summary>
public interface IVideoStorage
{
    /// <summary>
    /// Writes <paramref name="content"/> to storage under a generated name and
    /// returns that name, which is what the Videos table stores.
    /// </summary>
    /// <param name="content">Stream positioned at the start of the file.</param>
    /// <param name="extension">File extension including the dot, lower case.</param>
    Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default);

    /// <summary>Opens a stored file for reading, or returns null when it is gone.</summary>
    Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken = default);

    /// <summary>Removes a stored file. Does nothing when the file is already gone.</summary>
    Task DeleteAsync(string storedFileName, CancellationToken cancellationToken = default);

    /// <summary>True when the file is still in storage.</summary>
    bool Exists(string storedFileName);
}
