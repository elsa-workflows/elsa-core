using System.Text.Json.Nodes;
using Elsa.Extensions;
using Elsa.OrchardCore.Client.Contracts;
using Elsa.OrchardCore.Client.Models;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.OrchardCore.Activities;

/// <summary>
/// Represents an activity that resolves a set of tags into their corresponding tag content items. If a tag does not exist, it is created.
/// </summary>
[Activity("OrchardCore", "Orchard Core", "Returns tag content items for the specified set of tags. If a tag doesn't exist, it is created.", Kind = ActivityKind.Task)]
public class ResolveTags : CodeActivity<JsonObject>
{
    /// <summary>
    /// Gets or sets the collection of tags to resolve.
    /// The activity attempts to resolve each tag into its corresponding tag content item.
    /// If a tag does not exist, it is created.
    /// </summary>
    [Input(Description = "The tags to resolve.")]
    public Input<ICollection<string>> Tags { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var tags = Tags.Get(context);
        var apiClient = context.GetRequiredService<IRestApiClient>();
        var request = new ResolveTagsRequest
        {
            Tags = tags
        };
        var result = await apiClient.ResolveTagsAsync(request, context.CancellationToken);
        context.SetResult(result);
    }
}