using System.Text.Json;
using Elsa.Extensions;
using Elsa.OrchardCore.Client;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.OrchardCore.Activities;

public class SendGraphQLQuery : CodeActivity<JsonElement>
{
    /// The content type to handle the event for.
    [Input(Description = "The GraphQL query string to send.")]
    public Input<string> Query { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var query = Query.Get(context);
        var client = context.GetRequiredService<IGraphQLClient>();
        var element = await client.SendQueryAsync(query, context.CancellationToken);
        context.SetResult(element);
    }
}