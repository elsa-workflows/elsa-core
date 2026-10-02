using System.Text.Json.Nodes;
using Elsa.Extensions;
using Elsa.OrchardCore.Client.Contracts;
using Elsa.OrchardCore.Client.Models;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.OrchardCore.Activities;

/// <summary>
/// Represents an activity for patching an Orchard Core content item.
/// </summary>
/// <remarks>
/// This activity sends a patch request for a specified content item ID, applying the provided patch
/// and optionally publishing the patched content item.
/// </remarks>
[Activity("OrchardCore", "Orchard Core", "Patches a content item", Kind = ActivityKind.Task)]
public class PatchContentItem : CodeActivity<JsonObject>
{
    /// <summary>
    /// Gets or sets the ID of the content item to patch.
    /// </summary>
    /// <remarks>
    /// This property specifies the unique identifier for the target content item to be patched.
    /// It is used to locate the content item within the Orchard Core system.
    /// </remarks>
    [Input(Description = "The ID of the content item to patch.")]
    public Input<string> ContentItemId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the patch to apply to the content item.
    /// </summary>
    /// <remarks>
    /// This property specifies the JSON object representing the modifications to be applied to the target content item.
    /// It enables partial updates of the content item's properties.
    /// </remarks>
    [Input(Description = "The patch to apply to the content item.")]
    public Input<JsonObject> Patch { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether to publish the patched content item.
    /// </summary>
    /// <remarks>
    /// This property determines if the patched content item should be published after the patch operation is applied.
    /// If set to true, the content item will be published; otherwise, it will remain unpublished.
    /// </remarks>
    [Input(Description = "Whether to publish the patched content item.")]
    public Input<bool> Publish { get; set; } = null!;

    /// <inheritdoc />
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