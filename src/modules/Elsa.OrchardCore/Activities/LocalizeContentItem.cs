using System.Text.Json.Nodes;
using Elsa.Extensions;
using Elsa.OrchardCore.Client;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.OrchardCore.Activities;

[Activity("OrchardCore", "Orchard Core", "Localizes a content item", Kind = ActivityKind.Task)]
public class LocalizeContentItem : CodeActivity<JsonObject>
{
    [Input(Description = "The ID of the content item to localize.")]
    public Input<string> ContentItemId { get; set; } = default!;

    [Input(Description = "The culture code to use when creating a localized version.")]
    public Input<string> CultureCode { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var contentItemId = ContentItemId.Get(context);
        var cultureCode = CultureCode.Get(context);
        var apiClient = context.GetRequiredService<IRestApiClient>();
        var request = new LocalizeContentItemRequest
        {
            CultureCode = cultureCode
        };
        var localizedContentItem = await apiClient.LocalizeContentItemAsync(contentItemId, request, context.CancellationToken);
        context.SetResult(localizedContentItem);
    }
}