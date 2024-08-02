using System.Text.Json.Nodes;
using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.OrchardCore.Client;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.OrchardCore.Activities;

[Activity("OrchardCore", "Orchard Core", "Patches a content item", Kind = ActivityKind.Task)]
public class CreateContentItem : CodeActivity<JsonObject>
{
    [Input(Description = "The content type of the content item to create.")]
    public Input<string> ContentType { get; set; } = default!;

    [Input(Description = "The properties to apply to the content item.")]
    public Input<object> Properties { get; set; } = default!;

    [Input(Description = "Whether to publish the content item.")]
    public Input<bool> Publish { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var contentType = ContentType.Get(context);
        var publish = Publish.Get(context);
        var properties = Properties.Get(context).ConvertTo<JsonObject>()!;
        var apiClient = context.GetRequiredService<IRestApiClient>();
        var request = new CreateContentItemRequest
        {
            ContentType = contentType,
            Properties = properties,
            Publish = publish
        };
        var patchedContentItem = await apiClient.CreateContentItemAsync(request, context.CancellationToken);
        context.SetResult(patchedContentItem);
    }
}