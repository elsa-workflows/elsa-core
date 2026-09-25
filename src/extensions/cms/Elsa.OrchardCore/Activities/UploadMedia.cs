using System.Text.Json.Nodes;
using Elsa.Extensions;
using Elsa.Http;
using Elsa.OrchardCore.Client.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.OrchardCore.Activities;

/// <summary>
/// Represents an activity that uploads files to Orchard Core's media library.
/// This activity interacts with a REST API client to execute the upload process.
/// </summary>
[Activity("OrchardCore", "Orchard Core", "Upload files to Orchard Core's media library.", Kind = ActivityKind.Task)]
public class UploadMedia : CodeActivity<JsonObject>
{
    /// <summary>
    /// Represents the collection of files to be uploaded to the media library
    /// during the execution of the UploadMedia activity.
    /// </summary>
    /// <remarks>
    /// This property is an input parameter for specifying the files that should be
    /// uploaded to the Orchard Core media library. The files can be provided at runtime
    /// using an ICollection of HttpFile objects.
    /// </remarks>
    [Input(Description = "The files to upload into the Media Library.")]
    public Input<ICollection<HttpFile>> Files { get; set; } = null!;

    /// <summary>
    /// Specifies the target folder path in the Orchard Core media library where
    /// the files should be uploaded during the execution of the UploadMedia activity.
    /// </summary>
    /// <remarks>
    /// This property serves as an input parameter to define the specific folder
    /// in the media library for file uploads. If not provided, the files will be
    /// uploaded to the root of the media library by default.
    /// It accepts a nullable string value representing the folder path.
    /// </remarks>
    [Input(Description = "The path to the media library folder to upload.")]
    public Input<string?> FolderPath { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var files = Files.Get(context);
        var folderPath = FolderPath.GetOrDefault(context);
        var apiClient = context.GetRequiredService<IRestApiClient>();
        var response = await apiClient.UploadFilesAsync(files, folderPath, context.CancellationToken);
        context.SetResult(response);
    }
}