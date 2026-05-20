using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Reflection;
using Elsa.Common;
using Elsa.Http;
using Elsa.Http.FileCaches;
using Elsa.Http.Options;
using FluentStorage;
using FluentStorage.Blobs;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.Activities.UnitTests.Http;

public class ZipManagerTests : IDisposable
{
    private static readonly Type ZipManagerType = typeof(WriteFileHttpResponse).Assembly.GetRequiredType("Elsa.Http.Services.ZipManager");
    private static readonly MethodInfo CreateAsyncMethod = ZipManagerType.GetRequiredMethod("CreateAsync");
    private static readonly MethodInfo LoadAsyncMethod = ZipManagerType.GetRequiredMethod("LoadAsync");
    private readonly string _cacheDirectory = Path.Join(Path.GetTempPath(), "elsa-zip-manager-tests", Guid.NewGuid().ToString("N"));
    private readonly IBlobStorage _blobStorage;
    private readonly object _zipManager;
    private readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public ZipManagerTests()
    {
        Directory.CreateDirectory(_cacheDirectory);
        _blobStorage = StorageFactory.Blobs.DirectoryFiles(_cacheDirectory);
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

    [Theory]
    [InlineData("../download")]
    [InlineData("..\\download")]
    [InlineData("nested/download")]
    [InlineData("nested\\download")]
    [InlineData("download.token")]
    [InlineData("download token")]
    public async Task LoadAsync_TraversalLikeToken_ReturnsNull(string downloadId)
    {
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

    public void Dispose()
    {
        if (Directory.Exists(_cacheDirectory))
            Directory.Delete(_cacheDirectory, true);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2077:Target type is resolved by name for an internal test subject.")]
    private object CreateZipManager()
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(_now);
        var fileCacheStorageProvider = new BlobFileCacheStorageProvider(_blobStorage);
        var options = Options.Create(new HttpFileCacheOptions
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

    private async Task<(Blob Blob, byte[] Content)?> LoadAsync(string downloadId)
    {
        var task = (Task)LoadAsyncMethod.Invoke(_zipManager, [downloadId, CancellationToken.None])!;
        await task;
        var result = task.GetType().GetRequiredProperty("Result").GetValue(task);

        if (result == null)
            return null;

        var blob = (Blob)result.GetType().GetRequiredField("Item1").GetValue(result)!;
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
