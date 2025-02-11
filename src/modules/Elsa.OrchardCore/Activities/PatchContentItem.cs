using System.Text.Json.Nodes;
using Elsa.Extensions;
using Elsa.OrchardCore.Client;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.OrchardCore.Activities;

[Activity("OrchardCore", "Orchard Core", "Patches a content item", Kind = ActivityKind.Task)]
public class PatchContentItem : CodeActivity<JsonObject>
{
    [Input(Description = "The ID of the content item to patch.")]
    public Input<string> ContentItemId { get; set; } = default!;

    [Input(Description = "The patch to apply to the content item.")]
    public Input<JsonObject> Patch { get; set; } = default!;

    [Input(Description = "Whether to publish the patched content item.")]
    public Input<bool> Publish { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var contentItemId = ContentItemId.Get(context);
        var publish = Publish.Get(context);
        var patch = Patch.Get(context);
        var apiClient = context.GetRequiredService<IRestApiClient>();
        var request = new PatchContentItemRequest
        {
            Patch = patch,
            Publish = publish
        };
        var patchedContentItem = await apiClient.PatchContentItemAsync(contentItemId, request, context.CancellationToken);
        context.SetResult(patchedContentItem);
    }
}