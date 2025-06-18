using System.ComponentModel;
using System.IO.Compression;
using System.Text.Json.Serialization;
using Elsa.IO.Compression.Models;
using Elsa.IO.Services;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.IO.Compression.Activities;

/// <summary>
/// Creates a ZIP archive from a collection of entries.
/// </summary>
[Activity("Elsa", "Compression", "Creates a ZIP archive from a collection of entries.", DisplayName = "Create Zip Archive")]
public class CreateZipArchive : Activity<Stream>
{
    /// <inheritdoc />
    [JsonConstructor]
    public CreateZipArchive(string? source = null, int? line = null) : base(source, line)
    {
    }

    /// <summary>
    /// The filename for the ZIP archive.
    /// </summary>
    [Description("The filename for the ZIP archive.")]
    public Input<string> Filename { get; set; } = null!;

    /// <summary>
    /// The entries to include in the ZIP archive. Can be byte[], Stream, file path, file URL, base64 string, ZipEntry objects, or arrays of these types.
    /// </summary>
    [Description("The entries to include in the ZIP archive. Can be byte[], Stream, file path, file URL, base64 string, ZipEntry objects, or arrays of these types.")]
    public Input<object?> Entries { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var filename = Filename.Get(context) ?? "archive.zip";
        var entriesInput = Entries.GetOrDefault(context);
        var resolver = context.GetRequiredService<IContentResolver>();
        var logger = context.GetRequiredService<ILogger<CreateZipArchive>>();

        // Parse entries from various input types
        var entries = ParseEntries(entriesInput);

        // Create a memory stream to hold the ZIP archive
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
                        var (stream, resolvedName) = await resolver.ResolveContentAsync(zipEntry.Content, zipEntry.EntryName, context.CancellationToken);
                        contentStream = stream;
                        entryName = resolvedName;
                    }
                    else
                    {
                        var (stream, resolvedName) = await resolver.ResolveContentAsync(entryContent, null, context.CancellationToken);
                        contentStream = stream;
                        entryName = resolvedName.Length > 0 ? resolvedName : $"entry-{entryIndex}.bin";
                    }

                    // Create the zip entry
                    var archiveEntry = zipArchive.CreateEntry(entryName);
                    
                    // Copy content to the zip entry
                    await using var entryStream = archiveEntry.Open();
                    await contentStream.CopyToAsync(entryStream, context.CancellationToken);
                    await entryStream.FlushAsync(context.CancellationToken);

                    // Dispose the content stream if we created it
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
                    // Continue with next entry
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create ZIP archive");
            zipStream.Dispose();
            throw;
        }

        // Reset stream position for reading
        zipStream.Position = 0;
        
        // Set the result
        context.Set(Result, zipStream);
    }

    private static IEnumerable<object> ParseEntries(object? entriesInput)
    {
        if (entriesInput == null)
            return Array.Empty<object>();

        // Handle single entry
        if (entriesInput is not IEnumerable<object> enumerable)
            return new[] { entriesInput };

        // Handle array of entries
        return enumerable;
    }
}