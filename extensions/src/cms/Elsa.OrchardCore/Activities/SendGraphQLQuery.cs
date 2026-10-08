using Elsa.Extensions;
using Elsa.OrchardCore.Client.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.OrchardCore.Activities;

/// <summary>
/// Represents a task activity that sends a GraphQL query to an Orchard Core server
/// and processes the response.
/// </summary>
/// <remarks>
/// This activity is part of the Orchard Core integration and allows workflows
/// to interact with a GraphQL API by sending a query string.
/// </remarks>
[Activity("OrchardCore", "Orchard Core", "Send a GraphQL query to Orchard Core", DisplayName = "GraphQL Query", Kind = ActivityKind.Task)]
public class SendGraphQLQuery : CodeActivity<object>
{
    /// <summary>
    /// The content type to handle the event for.
    /// </summary>
    [Input(Description = "The GraphQL query string to send.")]
    public Input<string> Query { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var query = Query.Get(context);
        var client = context.GetRequiredService<IGraphQLClient>();
        var targetType = Result.GetTargetType(context);
        var output = await client.SendQueryAsync(query, targetType, context.CancellationToken);
        context.SetResult(output);
    }
}