using System.Text.Json.Nodes;
using Elsa.Extensions;
using Elsa.Http;
using Elsa.OrchardCore.Client;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.OrchardCore.Activities;

[Activity("OrchardCore", "Orchard Core", "Patches a content item", Kind = ActivityKind.Task)]
public class UploadMedia : CodeActivity<JsonObject>
{
    [Input(Description = "The files to upload into the Media Library.")]
    public Input<ICollection<HttpFile>> Files { get; set; } = default!;

    [Input(Description = "The path to the media library folder to upload.")]
    public Input<string?> FolderPath { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var files = Files.Get(context);
        var folderPath = FolderPath.GetOrDefault(context);
        var apiClient = context.GetRequiredService<IRestApiClient>();
        var response = await apiClient.UploadFilesAsync(files, folderPath, context.CancellationToken);
        context.SetResult(response);
    }
}