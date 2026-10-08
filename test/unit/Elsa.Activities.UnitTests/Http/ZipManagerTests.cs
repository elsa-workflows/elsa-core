using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Elsa.Common;
using Elsa.Http;
using Elsa.Http.FileCaches;
using Elsa.Http.Options;
using FluentStorage;
using FluentStorage.Enums;
using FluentStorage.Model;
using FluentStorage.Rules;
using FluentStorage.Storage;
using FluentStorage.Streaming;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.Activities.UnitTests.Http;

public class ZipManagerTests : IDisposable
{
    private static readonly Type ZipManagerType = typeof(WriteFileHttpResponse).Assembly.GetRequiredType("Elsa.Http.Services.ZipManager");
    private static readonly MethodInfo CreateAsyncMethod = ZipManagerType.GetRequiredMethod("CreateAsync");
    private static readonly MethodInfo LoadAsyncMethod = ZipManagerType.GetRequiredMethod("LoadAsync");
    private readonly string _cacheDirectory = Path.Join(Path.GetTempPath(), "elsa-zip-manager-tests", Guid.NewGuid().ToString("N"));
    private readonly IStore _blobStorage;
    private readonly object _zipManager;
    private readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public ZipManagerTests()
    {
        Directory.CreateDirectory(_cacheDirectory);
        _blobStorage = StorageFactory.Disk(_cacheDirectory);
        _zipManager = CreateZipManager();
    }

    [Fact]
    public async Task LoadAsync_ValidToken_LoadsCachedZip()
    {
        var downloadId = "download_ABC-123";
        await CreateCachedZipAsync(downloadId, "cached zip");

        var result = await LoadAsync(downloadId);

        Assert.True(result.HasValue);
        var value = result.Value;
        using var zipStream = new MemoryStream(value.Content);
        using var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read);
        var entry = Assert.Single(zipArchive.Entries);
        await using var entryStream = entry.Open();
        using var reader = new StreamReader(entryStream);
        Assert.Equal("cached zip", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task LoadAsync_ValidTokenWithDots_LoadsCachedZip()
    {
        var downloadId = "Elsa.Alterations.ExecuteAlterationPlan";
        await CreateCachedZipAsync(downloadId, "cached zip");

        var result = await LoadAsync(downloadId);

        Assert.True(result.HasValue);
    }

    [Fact]
    public async Task LoadAsync_ValidToken_OpensReturnedBlobFullPathWithinCacheDirectory()
    {
        var provider = Substitute.For<IFileCacheStorageProvider>();
        var blobPath = Path.Join(_cacheDirectory, "download.tmp");
        var blobStorage = new BlobStorageStub("download.tmp", blobPath, [1, 2, 3], _now.AddMinutes(5));
        var zipManager = CreateZipManager(provider);
        provider.GetStorage().Returns(blobStorage);

        var result = await LoadAsync("download", zipManager);

        Assert.True(result.HasValue);
        Assert.Equal([1, 2, 3], result.Value.Content);
        Assert.Equal(blobPath, blobStorage.OpenedPath);
    }

    [Fact]
    public async Task LoadAsync_RootedPathOutsideCacheDirectory_ReturnsNull()
    {
        var provider = Substitute.For<IFileCacheStorageProvider>();
        var blobStorage = new BlobStorageStub("download.tmp", Path.Join(Path.GetTempPath(), "download.tmp"), [1, 2, 3], _now.AddMinutes(5));
        var zipManager = CreateZipManager(provider);
        provider.GetStorage().Returns(blobStorage);

        var result = await LoadAsync("download", zipManager);

        Assert.Null(result);
        Assert.Null(blobStorage.OpenedPath);
    }

    [Theory]
    [InlineData("../download")]
    [InlineData("..\\download")]
    [InlineData("nested/download")]
    [InlineData("nested\\download")]
    [InlineData("download token")]
    public async Task LoadAsync_TraversalLikeToken_ReturnsNull(string downloadId)
    {
        var result = await LoadAsync(downloadId);

        Assert.Null(result);
    }

    [Fact]
    public async Task LoadAsync_TooLongToken_ReturnsNull()
    {
        var downloadId = new string('a', 129);

        var result = await LoadAsync(downloadId);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("../download")]
    [InlineData("..\\download")]
    public async Task CreateAsync_TraversalLikeToken_DoesNotCacheFile(string downloadId)
    {
        await CreateCachedZipAsync(downloadId, "cached zip");

        Assert.Empty(Directory.EnumerateFileSystemEntries(_cacheDirectory));
    }

    [Fact]
    public async Task CreateAsync_TooLongToken_DoesNotCacheFile()
    {
        var downloadId = new string('a', 129);

        await CreateCachedZipAsync(downloadId, "cached zip");

        Assert.Empty(Directory.EnumerateFileSystemEntries(_cacheDirectory));
    }

    public void Dispose()
    {
        if (Directory.Exists(_cacheDirectory))
            Directory.Delete(_cacheDirectory, true);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2077:Target type is resolved by name for an internal test subject.")]
    private object CreateZipManager(IFileCacheStorageProvider? fileCacheStorageProvider = null)
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(_now);
        fileCacheStorageProvider ??= new BlobFileCacheStorageProvider(_blobStorage);
        var options = Microsoft.Extensions.Options.Options.Create(new HttpFileCacheOptions
        {
            LocalCacheDirectory = _cacheDirectory,
            TimeToLive = TimeSpan.FromHours(1)
        });
        var logger = typeof(NullLogger<>).MakeGenericType(ZipManagerType).GetRequiredField("Instance").GetValue(null)!;

        return Activator.CreateInstance(ZipManagerType, clock, fileCacheStorageProvider, options, logger)!;
    }

    private async Task CreateCachedZipAsync(string downloadId, string content)
    {
        var downloadables = new List<Func<ValueTask<Downloadable>>>
        {
            () => ValueTask.FromResult(new Downloadable
            {
                Stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)),
                Filename = "file.txt"
            })
        };
        var task = (Task)CreateAsyncMethod.Invoke(_zipManager, [downloadables, true, downloadId, "download.zip", "application/zip", CancellationToken.None])!;
        await task;
        var result = task.GetType().GetRequiredProperty("Result").GetValue(task)!;
        var zipStream = (Stream)result.GetType().GetRequiredField("Item2").GetValue(result)!;
        var cleanup = (Action)result.GetType().GetRequiredField("Item3").GetValue(result)!;

        try
        {
            await zipStream.DisposeAsync();
        }
        finally
        {
            cleanup();
        }
    }

    private async Task<(StoreObject Blob, byte[] Content)?> LoadAsync(string downloadId, object? zipManager = null)
    {
        var task = (Task)LoadAsyncMethod.Invoke(zipManager ?? _zipManager, [downloadId, CancellationToken.None])!;
        await task;
        var result = task.GetType().GetRequiredProperty("Result").GetValue(task);

        if (result == null)
            return null;

        var blob = (StoreObject)result.GetType().GetRequiredField("Item1").GetValue(result)!;
        await using var stream = (Stream)result.GetType().GetRequiredField("Item2").GetValue(result)!;
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        return (blob, memoryStream.ToArray());
    }
}

internal static class ReflectionExtensions
{
    public static Type GetRequiredType(this Assembly assembly, string typeName)
    {
        return assembly.GetType(typeName) ?? throw new InvalidOperationException($"Could not find type {typeName}.");
    }

    public static MethodInfo GetRequiredMethod(this Type type, string methodName)
    {
        return type.GetMethod(methodName) ?? throw new InvalidOperationException($"Could not find method {type.FullName}.{methodName}.");
    }

    public static PropertyInfo GetRequiredProperty(this Type type, string propertyName)
    {
        return type.GetProperty(propertyName) ?? throw new InvalidOperationException($"Could not find property {type.FullName}.{propertyName}.");
    }

    public static FieldInfo GetRequiredField(this Type type, string fieldName)
    {
        return type.GetField(fieldName) ?? throw new InvalidOperationException($"Could not find field {type.FullName}.{fieldName}.");
    }
}

internal sealed class BlobStorageStub(string lookupPath, string fullPath, byte[] content, DateTimeOffset expiresAt) : IStore
{
    public string? OpenedPath { get; private set; }

    public Task<Stream> OpenRead(string fullPath, CancellationToken cancellationToken = default)
    {
        OpenedPath = fullPath;
        return Task.FromResult(CreateContentStream());
    }

    public Task<StoreObject> GetObjectInfo(string objectPath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<StoreObject>(CreateBlob());
    }

    public Task<List<StoreObject>> GetObjectsInfo(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default)
    {
        var blobs = fullPaths.Contains(lookupPath)
            ? [CreateBlob()]
            : new List<StoreObject>();

        return Task.FromResult<List<StoreObject>>(blobs);
    }

    public void Dispose()
    {
    }

    private StoreObject CreateBlob()
    {
        var blob = new StoreObject(fullPath);
        blob.Metadata["ExpiresAt"] = expiresAt.ToString("O");
        return blob;
    }

    private Stream CreateContentStream() => new MemoryStream(content, writable: false);

    #region not implemented IStore interfacce impls

    public Task<object> GetClient() => throw new NotSupportedException();

    public Task<bool> IsFileSystem() => throw new NotSupportedException();

    public Task<bool> IsSeekable() => throw new NotSupportedException();

    public Task<bool> IsVersioned() => throw new NotSupportedException();

    public Task<bool> IsTagged() => throw new NotSupportedException();

    public Task<bool> IsTiered()
        => throw new NotSupportedException();

    public Task<List<StoreObject>> ListDirectory(string folderPath, bool recurse, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<List<StoreObject>> ListDirectory(string folderPath = null, Func<StoreObject, bool> browseFilter = null, string filePrefix = null, bool recurse = false, StorageRecursion recursionMode = StorageRecursion.Remote, int numberOfRecursionThreads = 10, int? maxResults = null, bool includeAttributes = false, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<List<StoreObject>> ListObjects(StorageListOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task SetObject(string fullPath, Stream dataStream, bool append = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<bool> ObjectExists(string objectPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<List<bool>> ObjectsExists(IEnumerable<string> objectPaths, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task SetObjectInfo(StoreObject metadata, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task SetObjectsInfo(IEnumerable<StoreObject> metadata, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<long> GetObjectLength(string path, long defaultValue = -1, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<Stream> OpenWrite(string objectPath, bool overwrite, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<Stream> OpenRange(string path, long offset, long length, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<SeekableStream> OpenSeekable(string path, int bufferSize = 65536, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task GetObject(string objectPath, Stream targetStream, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<byte[]> GetBytes(string objectPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<string> GetText(string objectPath, Encoding textEncoding = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<T> GetJson<T>(string objectPath, bool ignoreInvalidJson = false, JsonSerializerOptions options = null, Encoding encoding = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task DownloadObject(string objectPath, string filePath, bool overwrite, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task SetObject(string objectPath, Stream dataStream, string contentType, bool append = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task SetBytes(string objectPath, byte[] data, bool append = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task SetText(string objectPath, string text, Encoding textEncoding = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task SetJson<T>(string objectPath, T instance, JsonSerializerOptions options = null, Encoding encoding = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task UploadObject(string objectPath, string filePath, bool overwrite, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task CopyObjectTo(string blobId, IStore targetStorage, string newId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<bool> MoveObject(string oldPath, string newPath, bool overwrite, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task DeleteObject(string objectPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task DeleteObjects(IEnumerable<string> objectPaths, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task DeleteObjects(IEnumerable<StoreObject> blobs, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<List<StorageProgress>> DownloadDirectory(string remoteFolder, string localFolder, StorageExists existsMode = StorageExists.Skip, Action<StorageProgress>? progress = null, IList<StorageRule> rules = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<List<StorageProgress>> UploadDirectory(string localFolder, string remoteFolder, StorageExists existsMode = StorageExists.Skip, Action<StorageProgress>? progress = null, IList<StorageRule> rules = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<string> GetUploadUrl(string objectPath, bool https, int expiresInSeconds = 86000) => throw new NotSupportedException();

    public Task<string> GetDownloadUrl(string objectPath, bool https, int expiresInSeconds = 86000) => throw new NotSupportedException();

    public Task<string> GetPresignedUrl(string objectPath, bool forDownload, bool https, int expiresInSeconds = 86000) => throw new NotSupportedException();

    public Task<string> GetObjectSas(string objectPath, StorageUrlOptions options) => throw new NotSupportedException();

    public Task<Dictionary<string, object>> GetServer(CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task CreateDirectory(string folderPath, bool force, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task DeleteDirectory(string folderPath, bool recursive, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<bool> DirectoryExists(string folderPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task MoveDirectory(string sourceFolderPath, string destinationFolderPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<int> GetFilePermissions(string filePath, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task SetFilePermissions(string filePath, int permissions, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<List<StorageObjectVersion>> ListObjectVersions(string objectPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<StorageObjectVersion> GetObjectVersion(string objectPath, string versionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<bool> RestoreObjectVersion(string objectPath, string versionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<bool> DeleteObjectVersion(string objectPath, string versionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<Dictionary<string, string>> GetObjectTags(string objectPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<bool> SetObjectTags(string objectPath, Dictionary<string, string> tags, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<bool> DeleteObjectTags(string objectPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<StorageTier> GetObjectTier(string objectPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<bool> SetObjectTier(string objectPath, StorageTier tier, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<StorageObjectHash> GetObjectChecksum(string fullPath, StorageHash hash = StorageHash.MD5, CancellationToken cancellationToken = default) => throw new NotSupportedException();

#endregion
}
