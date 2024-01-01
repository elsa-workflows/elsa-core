using System.Collections;
using System.IO.Compression;
using System.Text;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.FileStorage.Activities;

/// <summary>
/// Save a file to the configured storage provider.
/// </summary>
[Activity("Elsa", "Storage", "Save a file to the configured storage provider.", Kind = ActivityKind.Task)]
public class SaveFile : CodeActivity
{
    /// <summary>
    /// Gets or sets the file data to save.
    /// </summary>
    [Input(Description = "The file data to save. This can be a stream, binary data, a string, a form file or a collection of files.")]
    public Input<object> Data { get; set; } = default!;

    /// <summary>
    /// Gets or sets the path to save the file to.
    /// </summary>
    [Input(Description = "The path to save the file to.")]
    public Input<string> Path { get; set; } = default!;

    /// <summary>
    /// Gets or sets a value indicating whether to append to the file if it already exists.
    /// </summary>
    [Input(Description = "Whether to append to the file if it already exists.")]
    public Input<bool> Append { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var data = await ResolveAsStreamAsync(Data.Get(context), cancellationToken);
        var path = Path.Get(context);
        var append = Append.GetOrDefault(context);
        var blobStorageProvider = context.GetRequiredService<IBlobStorageProvider>();
        var blobStorage = blobStorageProvider.GetBlobStorage();
        await blobStorage.WriteAsync(path, data, append, cancellationToken);
    }

    private async Task<Stream> ResolveAsStreamAsync(object data, CancellationToken cancellationToken)
    {
        if(data is Stream stream)
            return stream;
        
        if(data is byte[] bytes)
            return new MemoryStream(bytes);
        
        if(data is IFormFile formFile)
            return formFile.OpenReadStream();
        
        if(data is string stringData)
            return new MemoryStream(Encoding.UTF8.GetBytes(stringData));

        if (data is IEnumerable enumerable)
        {
            var files = enumerable.Cast<object>().ToList();
            return files.Count == 1 ? await ResolveAsStreamAsync(files[0], cancellationToken) : await CreateZipArchiveAsync(files, cancellationToken);
        }
        
        throw new NotSupportedException($"The provided data type is not supported: {data.GetType().Name}");
    }
    
    private async Task<Stream> CreateZipArchiveAsync(IEnumerable files, CancellationToken cancellationToken = default)
    {
        var currentFileIndex = 0;
        var zipStream = new MemoryStream();
        var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, true);
        
        foreach (var file in files)
        {
            var entryName = $"file-{currentFileIndex}.bin";
            var entry = zipArchive.CreateEntry(entryName);
            var fileStream = await ResolveAsStreamAsync(file, cancellationToken);
            await using var entryStream = entry.Open();
            await fileStream.CopyToAsync(entryStream, cancellationToken);
            await entryStream.FlushAsync(cancellationToken);
            entryStream.Close();
            currentFileIndex++;
        }

        zipStream.Seek(0, SeekOrigin.Begin);
        return zipStream;
    }
}