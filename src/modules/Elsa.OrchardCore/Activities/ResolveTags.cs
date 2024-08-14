using System.Text.Json.Nodes;
using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.OrchardCore.Client;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.OrchardCore.Activities;

[Activity("OrchardCore", "Orchard Core", "Returns tag content items for the specified set of tags. If a tag doesn't exist, it is created.", Kind = ActivityKind.Task)]
public class ResolveTags : CodeActivity<JsonObject>
{
    [Input(Description = "The tags to resolve.")]
    public Input<ICollection<string>> Tags { get; set; } = default!;

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