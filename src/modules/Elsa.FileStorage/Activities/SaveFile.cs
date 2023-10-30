using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;

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
    public Input<Stream> Data { get; set; } = default!;

    /// <summary>
    /// Gets or sets the path to save the file to.
    /// </summary>
    public Input<string> Path { get; set; } = default!;

    /// <summary>
    /// Gets or sets a value indicating whether to append to the file if it already exists.
    /// </summary>
    public Input<bool> Append { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var data = Data.Get(context);
        var path = Path.Get(context);
        var append = Append.GetOrDefault(context);
        var blobStorageProvider = context.GetRequiredService<IBlobStorageProvider>();
        var blobStorage = blobStorageProvider.GetBlobStorage();
        await blobStorage.WriteAsync(path, data, append, cancellationToken);
    }
}