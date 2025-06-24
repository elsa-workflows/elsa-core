using System.IO.Compression;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.IO.Compression.Models;
using Elsa.IO.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using Microsoft.Extensions.Logging;

namespace Elsa.IO.Compression.Activities;

/// <summary>
/// Creates a ZIP archive from a collection of entries.
/// </summary>
[Activity("Elsa", "Compression", "Creates a ZIP archive from a collection of entries.", DisplayName = "Create Zip Archive")]
public class CreateZipArchive : CodeActivity<Stream>
{
    /// <inheritdoc />
    [JsonConstructor]   
    public CreateZipArchive(string? source = null, int? line = null) : base(source, line)
    {
    }

    /// <summary>
    /// The filename for the ZIP archive.
    /// </summary>
    [Input(Description = "The filename for the ZIP archive.")]
    public Input<string> Filename { get; set; } = null!;

    /// <summary>
    /// The entries to include in the ZIP archive. Can be byte[], Stream, file path, file URL, base64 string, ZipEntry objects, or arrays of these types.
    /// </summary>
    [Input(
        Description = "The entries to include in the ZIP archive. Can be byte[], Stream, file path, file URL, base64 string, ZipEntry objects, or arrays of these types",
        UIHint = InputUIHints.MultiLine
    )]
    public Input<object?> Entries { get; set; } = null!;

    /// <summary>
    /// The filename that was used for this ZIP archive.
    /// </summary>
    [Output(Description = "The filename that was used for this ZIP archive.")]
    public Output<string> ResultFilename { get; set; } = new();
    
    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var entriesInput = Entries.Get(context);
        var resolver = context.GetRequiredService<IContentResolver>();
        var extensionResolver = context.GetRequiredService<IExtensionResolver>();
        var logger = context.GetRequiredService<ILogger<CreateZipArchive>>();
        
        var entries = ParseEntries(entriesInput);
        
        var zipStream = new MemoryStream();

        try
        {
            using var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true);
            
            var entryIndex = 0;

            foreach (var entryContent in entries)
            {
                try
                {
                    string entryName;
                    Stream contentStream;

                    if (entryContent is ZipEntry zipEntry)
                    {
                        // For ZipEntry objects, use the provided name
                        contentStream = await resolver.ResolveContentAsync(zipEntry.Content, context.CancellationToken);
                        entryName = !string.IsNullOrEmpty(zipEntry.EntryName) ? zipEntry.EntryName : $"entry-{entryIndex}";
                    }
                    else
                    {
                        // For all other types, use a generic entry name
                        contentStream = await resolver.ResolveContentAsync(entryContent, context.CancellationToken);
                        entryName = $"entry_{entryIndex+1}";
                    }
                    
                    entryName = CreateZipArchive.GetEntryNameWithExtension(entryName, entryContent, extensionResolver);
                    
                    var archiveEntry = zipArchive.CreateEntry(entryName, CompressionLevel.Optimal);
                    
                    await using var entryStream = archiveEntry.Open();
                    await contentStream.CopyToAsync(entryStream, context.CancellationToken);
                    await entryStream.FlushAsync(context.CancellationToken);
                    
                    if (entryContent is not Stream)
                    {
                        await contentStream.DisposeAsync();
                    }

                    entryIndex++;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to add entry {EntryIndex} to ZIP archive", entryIndex);
                    entryIndex++;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create ZIP archive");
            await zipStream.DisposeAsync();
            throw;
        }

        // Reset stream position for reading
        zipStream.Position = 0;
        
        SetFileName(context);
        
        Result.Set(context, zipStream);
    }

    private void SetFileName(ActivityExecutionContext context)
    {
        var filename = Filename?.Get(context) ?? "archive.zip";

        if (!filename.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            filename += ".zip";
        }
        
        ResultFilename.Set(context, filename);
    }
    
    private static string GetEntryNameWithExtension(string entryName, object content, IExtensionResolver extensionResolver)
    {
        if (Path.HasExtension(entryName))
        {
            return entryName;
        }
        
        if (content is not ZipEntry zipEntry)
        {
            return extensionResolver.EnsureFileExtension(entryName, content);
        }
        
        var tempName = extensionResolver.EnsureFileExtension(zipEntry.EntryName ?? "temp", zipEntry.Content);
        var extension = Path.GetExtension(tempName);
        
        entryName += extension;
        return entryName;
    }

    private static IEnumerable<object> ParseEntries(object? entriesInput)
    {
        return entriesInput switch
        {
            null => [],
            IEnumerable<object> enumerable => enumerable,
            Array array => array.Cast<object>(),
            _ => [entriesInput]
        };
    }
}