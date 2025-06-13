using System.ComponentModel;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Compression.Models;
using Elsa.Compression.Services;
using Elsa.Expressions.Models;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Compression.Activities;

/// <summary>
/// Creates a ZIP archive from a collection of entries.
/// </summary>
[Activity("Elsa", "Compression", "Creates a ZIP archive from a collection of entries.", DisplayName = "Create Zip Archive")]
public class CreateZipArchive : Activity<Stream>
{
    /// <inheritdoc />
    [JsonConstructor]
    private CreateZipArchive(string? source = null, int? line = null) : base(source, line)
    {
    }

    /// <inheritdoc />
    public CreateZipArchive(string filename, IEnumerable<object> entries, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) 
        : this(new Literal<string>(filename), new Literal<IEnumerable<object>>(entries), source, line)
    {
    }

    /// <inheritdoc />
    public CreateZipArchive(Func<string> filename, Func<IEnumerable<object>> entries, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) 
        : this(Expression.DelegateExpression(filename), Expression.DelegateExpression(entries), source, line)
    {
    }

    /// <inheritdoc />
    public CreateZipArchive(Func<ExpressionExecutionContext, string?> filename, Func<ExpressionExecutionContext, IEnumerable<object>?> entries, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) 
        : this(Expression.DelegateExpression(filename), Expression.DelegateExpression(entries), source, line)
    {
    }

    /// <inheritdoc />
    public CreateZipArchive(Variable<string> filename, Variable<IEnumerable<object>> entries, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) 
        : this(source, line)
    {
        Filename = new Input<string>(filename);
        Entries = new Input<IEnumerable<object>>(entries);
    }

    /// <inheritdoc />
    public CreateZipArchive(Literal<string> filename, Literal<IEnumerable<object>> entries, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) 
        : this(source, line)
    {
        Filename = new Input<string>(filename);
        Entries = new Input<IEnumerable<object>>(entries);
    }

    /// <inheritdoc />
    public CreateZipArchive(Expression filename, Expression entries, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) 
        : this(source, line)
    {
        Filename = new Input<string>(filename, new MemoryBlockReference());
        Entries = new Input<IEnumerable<object>>(entries, new MemoryBlockReference());
    }

    /// <inheritdoc />
    public CreateZipArchive(Input<string> filename, Input<IEnumerable<object>> entries, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) 
        : this(source, line)
    {
        Filename = filename;
        Entries = entries;
    }

    /// <summary>
    /// The filename for the ZIP archive.
    /// </summary>
    [Description("The filename for the ZIP archive.")]
    public Input<string> Filename { get; set; } = null!;

    /// <summary>
    /// The entries to include in the ZIP archive. Can be byte[], Stream, file path, file URL, base64 string, or ZipEntry objects.
    /// </summary>
    [Description("The entries to include in the ZIP archive. Can be byte[], Stream, file path, file URL, base64 string, or ZipEntry objects.")]
    public Input<IEnumerable<object>> Entries { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var filename = context.Get(Filename) ?? "archive.zip";
        var entries = context.Get(Entries) ?? Array.Empty<object>();
        var resolver = context.GetRequiredService<IZipEntryContentResolver>();
        var logger = context.GetRequiredService<ILogger<CreateZipArchive>>();

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
                        contentStream = await resolver.ResolveContentAsync(zipEntry, context.CancellationToken);
                        entryName = zipEntry.EntryName;
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
}