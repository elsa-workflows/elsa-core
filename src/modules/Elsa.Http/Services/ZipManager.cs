using System.IO.Compression;
using Elsa.Common;
using Elsa.Http.Options;
using FluentStorage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Http.Services;

/// <summary>
/// Provides a helper service for zipping downloadable content.
/// </summary>
internal class ZipManager
{
    private const int MaxDownloadCorrelationIdLength = 128;
    private readonly ISystemClock _clock;
    private readonly IFileCacheStorageProvider _fileCacheStorageProvider;
    private readonly IOptions<HttpFileCacheOptions> _fileCacheOptions;
    private readonly ILogger<ZipManager> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZipManager"/> class.
    /// </summary>
    public ZipManager(ISystemClock clock, IFileCacheStorageProvider fileCacheStorageProvider, IOptions<HttpFileCacheOptions> fileCacheOptions, ILogger<ZipManager> logger)
    {
        _clock = clock;
        _fileCacheStorageProvider = fileCacheStorageProvider;
        _fileCacheOptions = fileCacheOptions;
        _logger = logger;
    }

    public async Task<(Blob, Stream, Action)> CreateAsync(
        ICollection<Func<ValueTask<Downloadable>>> downloadables,
        bool cache,
        string? downloadCorrelationId,
        string? downloadAsFilename = default, 
        string? contentType = default, 
        CancellationToken cancellationToken = default)
    {
        // Create a temporary file.
        var tempFilePath = GetTempFilePath();

        // Create a zip archive from the downloadables.
        await CreateZipArchiveAsync(tempFilePath, downloadables, cancellationToken);

        // Create a blob with metadata for resuming the download.
        var zipBlob = CreateBlob(tempFilePath, downloadAsFilename, contentType);

        // If resumable downloads are enabled, cache the file.
        if (cache && !string.IsNullOrWhiteSpace(downloadCorrelationId))
            await CreateCachedZipBlobAsync(tempFilePath, downloadCorrelationId, downloadAsFilename, contentType, cancellationToken);

        var zipStream = File.OpenRead(tempFilePath);
        return (zipBlob, zipStream, () => Cleanup(tempFilePath));
    }

    /// <summary>
    /// Loads a cached zip blob for the specified download correlation ID.
    /// </summary>
    /// <param name="downloadCorrelationId">The download correlation ID.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A tuple containing the blob and the stream.</returns>
    public async Task<(Blob, Stream)?> LoadAsync(string downloadCorrelationId, CancellationToken cancellationToken = default)
    {
        if (!TryGetCacheFilename(downloadCorrelationId, out var fileCacheFilename))
        {
            _logger.LogDebug("Rejected invalid zip download correlation ID");
            return null;
        }

        var fileCacheStorage = _fileCacheStorageProvider.GetStorage();
        var blob = await fileCacheStorage.GetBlobAsync(fileCacheFilename, cancellationToken);

        if (blob == null)
            return null;

        // Check if the blob has expired.
        var expiresAt = DateTimeOffset.Parse(blob.Metadata["ExpiresAt"]);

        if (_clock.UtcNow > expiresAt)
        {
            // File expired. Try to delete it.
            try
            {
                await fileCacheStorage.DeleteAsync(blob.FullPath, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to delete expired file {FullPath}", blob.FullPath);
            }

            return null;
        }

        if (!IsBlobPathSafe(blob.FullPath, fileCacheFilename))
        {
            _logger.LogWarning("Rejected unsafe cached zip blob path {FullPath}", blob.FullPath);
            return null;
        }

        var stream = await fileCacheStorage.OpenReadAsync(blob.FullPath, cancellationToken);
        return (blob, stream);
    }
    
    /// <summary>
    /// Creates a zip archive from the specified <see cref="Downloadable"/> instances.
    /// </summary>
    private async Task CreateZipArchiveAsync(string filePath, IEnumerable<Func<ValueTask<Downloadable>>> downloadables, CancellationToken cancellationToken = default)
    {
        var currentFileIndex = 0;

        // Write the zip archive to the temporary file.
        await using var tempFileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize: 4096, useAsync: true);

        using var zipArchive = new ZipArchive(tempFileStream, ZipArchiveMode.Create, true);
        foreach (var downloadableFunc in downloadables)
        {
            var downloadable = await downloadableFunc();
            var entryName = !string.IsNullOrWhiteSpace(downloadable.Filename) ? downloadable.Filename : $"file-{currentFileIndex}.bin";
            var entry = zipArchive.CreateEntry(entryName);
            var fileStream = downloadable.Stream;
            await using var entryStream = entry.Open();
            await fileStream.CopyToAsync(entryStream, cancellationToken);
            await entryStream.FlushAsync(cancellationToken);
            entryStream.Close();
            currentFileIndex++;
        }
    }

    /// <summary>
    /// Creates a cached zip blob for the specified file.
    /// </summary>
    /// <param name="localPath">The full path of the file to upload.</param>
    /// <param name="downloadCorrelationId">The download correlation ID.</param>
    /// <param name="downloadAsFilename">The filename to use when downloading the file.</param>
    /// <param name="contentType">The content type of the file.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    private async Task CreateCachedZipBlobAsync(string localPath, string downloadCorrelationId, string? downloadAsFilename = default, string? contentType = default, CancellationToken cancellationToken = default)
    {
        if (!TryGetCacheFilename(downloadCorrelationId, out var fileCacheFilename))
        {
            _logger.LogDebug("Rejected invalid zip download correlation ID");
            return;
        }

        var fileCacheStorage = _fileCacheStorageProvider.GetStorage();
        var expiresAt = _clock.UtcNow.Add(_fileCacheOptions.Value.TimeToLive);
        var cachedBlob = CreateBlob(fileCacheFilename, downloadAsFilename, contentType, expiresAt);
        await fileCacheStorage.WriteFileAsync(fileCacheFilename, localPath, cancellationToken);
        await fileCacheStorage.SetBlobAsync(cachedBlob, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Creates a blob for the specified file.
    /// </summary>
    /// <param name="fullPath">The full path of the file.</param>
    /// <param name="downloadAsFilename">The filename to use when downloading the file.</param>
    /// <param name="contentType">The content type of the file.</param>
    /// <param name="expiresAt">The date and time at which the file expires.</param>
    /// <returns>The blob.</returns>
    private Blob CreateBlob(string fullPath, string? downloadAsFilename, string? contentType, DateTimeOffset? expiresAt = default)
    {
        (downloadAsFilename, contentType) = GetDownloadableMetadata(downloadAsFilename, contentType);

        var now = _clock.UtcNow;
        
        var blob = new Blob(fullPath)
        {
            Metadata =
            {
                ["ContentType"] = contentType,
                ["Filename"] = downloadAsFilename
            },
            CreatedTime = now,
            LastModificationTime = now
        };
        
        if(expiresAt.HasValue)
            blob.Metadata["ExpiresAt"] = expiresAt.Value.ToString("O");

        return blob;
    }
    
    private (string downloadAsFilename, string contentType) GetDownloadableMetadata(string? contentType, string? downloadAsFilename)
    {
        contentType = !string.IsNullOrWhiteSpace(contentType) ? contentType : System.Net.Mime.MediaTypeNames.Application.Zip;
        downloadAsFilename = !string.IsNullOrWhiteSpace(downloadAsFilename) ? downloadAsFilename : "download.zip";
        
        return (downloadAsFilename, contentType);
    }
    
    private string GetTempFilePath()
    {
        var tempFileName = Path.GetRandomFileName();
        var tempFilePath = Path.Combine(_fileCacheOptions.Value.LocalCacheDirectory, tempFileName);
        return tempFilePath;
    }

    private bool TryGetCacheFilename(string downloadCorrelationId, out string fileCacheFilename)
    {
        fileCacheFilename = default!;

        if (!IsValidDownloadCorrelationId(downloadCorrelationId))
            return false;

        var candidateFilename = $"{downloadCorrelationId}.tmp";

        if (!IsCachePathSafe(candidateFilename))
            return false;

        fileCacheFilename = candidateFilename;
        return true;
    }

    private static bool IsValidDownloadCorrelationId(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxDownloadCorrelationIdLength)
            return false;

        var invalidCharacters = value.Where(c => c is not (>= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_'));
        return !invalidCharacters.Any();
    }

    private bool IsBlobPathSafe(string path, string expectedFilename)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        if (!Path.IsPathRooted(path))
            return IsCachePathSafe(path);

        var fullPath = Path.GetFullPath(path);
        var fullCacheDirectory = GetFullCacheDirectory();
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (fullPath.StartsWith(fullCacheDirectory, comparison))
            return true;

        var blobPath = path.Replace('\\', '/');
        return !blobPath.Contains("/../", StringComparison.Ordinal)
               && !blobPath.EndsWith("/..", StringComparison.Ordinal)
               && blobPath.EndsWith($"/{expectedFilename}", StringComparison.Ordinal);
    }

    private bool IsCachePathSafe(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        if (Path.IsPathRooted(path))
            return false;

        var fullCacheDirectory = GetFullCacheDirectory();
        var fullPath = Path.GetFullPath(Path.Join(fullCacheDirectory, path));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return fullPath.StartsWith(fullCacheDirectory, comparison);
    }

    private string GetFullCacheDirectory()
    {
        var cacheDirectory = Path.GetFullPath(_fileCacheOptions.Value.LocalCacheDirectory);
        return Path.EndsInDirectorySeparator(cacheDirectory) ? cacheDirectory : cacheDirectory + Path.DirectorySeparatorChar;
    }
    
    private void Cleanup(string filePath)
    {
        try
        {
            File.Delete(filePath);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Failed to delete temporary file {TempFilePath}", filePath);
        }
    }
}
