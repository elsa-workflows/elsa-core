using System.IO.Compression;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.IO.Contracts;
using Elsa.IO.Extensions;
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
    private const string DefaultArchiveName = "archive.zip";
    private const string ZipExtension = ".zip";
    private const string DefaultEntryNameFormat = "entry_{0}";
    
    /// <inheritdoc />
    [JsonConstructor]   
    public CreateZipArchive(string? source = null, int? line = null) : base(source, line)
    {
    }

    /// <summary>
    /// The entries to include in the ZIP archive. Can be byte[], Stream, file path, file URL, base64 string, ZipEntry objects, or arrays of these types.
    /// </summary>
    [Input(
        Description = "The entries to include in the ZIP archive. Can be byte[], Stream, file path, file URL, base64 string, ZipEntry objects, or arrays of these types",
        UIHint = InputUIHints.MultiLine
    )]
    public Input<object?> Entries { get; set; } = null!;
    
    /// <summary>
    /// The compression level for the Zip Entries. Default is Optimal
    /// </summary>
    [Input(
        Description = "The compression level for the Zip Entries. Default is Optimal",
        UIHint = InputUIHints.DropDown
    )]
    public Input<CompressionLevel> CompressionLevel { get; set; } = new(System.IO.Compression.CompressionLevel.Optimal);

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var entriesInput = Entries.Get(context);
        var resolver = context.GetRequiredService<IContentResolver>();
        var logger = context.GetRequiredService<ILogger<CreateZipArchive>>();

        var entries = ParseEntries(entriesInput);
        
        var zipStream = await CreateZipStreamFromEntries(entries, resolver, context, logger);
        
        Result.Set(context, zipStream);
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
    
    private async Task<Stream> CreateZipStreamFromEntries(
        IEnumerable<object> entries, 
        IContentResolver resolver,
        ActivityExecutionContext context,
        ILogger logger)
    {
        var zipStream = new MemoryStream();

        try
        {
            using var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Update, leaveOpen: true);
            var entryIndex = 0;

            var compressionLevel = CompressionLevel.Get(context);
            foreach (var entryContent in entries)
            {
                try
                {
                    await ProcessZipEntry(entryContent, zipArchive, resolver, context, entryIndex, compressionLevel);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to add entry {EntryIndex} to ZIP archive. Reason: {ExceptionMessage}", 
                        entryIndex, ex.Message);
                }
                entryIndex++;
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
        return zipStream;
    }

    /// <summary>
    /// Processes a single zip entry and adds it to the archive.
    /// </summary>
    private static async Task ProcessZipEntry(
        object entryContent,
        ZipArchive zipArchive,
        IContentResolver resolver,
        ActivityExecutionContext context,
        int entryIndex,
        CompressionLevel compressionLevel)
    {
        var binaryContent = await resolver.ResolveAsync(entryContent, context.CancellationToken);
    
        var entryName = binaryContent.Name?.GetNameAndExtension()
                        ?? string.Format(DefaultEntryNameFormat, entryIndex + 1);
        
        // Get a unique name following Windows convention
        entryName = GetUniqueEntryName(zipArchive, entryName);
    
        var archiveEntry = zipArchive.CreateEntry(entryName, compressionLevel);
        
        await using var entryStream = archiveEntry.Open();
        await binaryContent.Stream.CopyToAsync(entryStream, context.CancellationToken);
        await entryStream.FlushAsync(context.CancellationToken);
    
        if (entryContent is not Stream)
        {
            await binaryContent.Stream.DisposeAsync();
        }
    }
    
    private static string GetUniqueEntryName(ZipArchive zipArchive, string originalName)
    {
        var filenameWithoutExtension = Path.GetFileNameWithoutExtension(originalName);
        var extension = Path.GetExtension(originalName);
        
        var originalExists = false;
        var highestIndex = 0;
        
        foreach (var entry in zipArchive.Entries)
        {
            if (!entry.Name.Equals(originalName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            
            originalExists = true;
            
            var entryNameWithoutExtension = Path.GetFileNameWithoutExtension(entry.Name);
            var entryExtension = Path.GetExtension(entry.Name);
            
            // Only process entries with the same extension
            if (!entryExtension.Equals(extension, StringComparison.OrdinalIgnoreCase))
                continue;
                
            // Check if this entry follows our naming pattern
            highestIndex = HighestEntryNameIndex(entryNameWithoutExtension, filenameWithoutExtension, highestIndex);
        }
        
        if (!originalExists)
        {
            return originalName;
        }
        
        return $"{filenameWithoutExtension}({highestIndex + 1}){extension}";
    }

    private static int HighestEntryNameIndex(string entryNameWithoutExtension, string filenameWithoutExtension,
        int highestIndex)
    {
        if (!entryNameWithoutExtension.StartsWith(filenameWithoutExtension, StringComparison.OrdinalIgnoreCase) ||
            entryNameWithoutExtension.Length <= filenameWithoutExtension.Length ||
            entryNameWithoutExtension[filenameWithoutExtension.Length] != '(')
        {
            return highestIndex;
        }

        // Extract the number between parentheses
        var closingParenIndex = entryNameWithoutExtension.LastIndexOf(')');
        if (closingParenIndex <= filenameWithoutExtension.Length + 1)
        {
            return highestIndex;
        }

        var indexStr = entryNameWithoutExtension.Substring(
            filenameWithoutExtension.Length + 1,
            closingParenIndex - filenameWithoutExtension.Length - 1);
    
        if (int.TryParse(indexStr, out var index))
        {
            highestIndex = Math.Max(highestIndex, index);
        }

        return highestIndex;
    }
}