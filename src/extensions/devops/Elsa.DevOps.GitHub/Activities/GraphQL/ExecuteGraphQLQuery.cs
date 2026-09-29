using Elsa.DevOps.GitHub.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using System.Text.Json;

namespace Elsa.DevOps.GitHub.Activities.GraphQL;

/// <summary>
/// Performs an authorized GraphQL query against the GitHub API.
/// </summary>
[Activity(
    "Elsa.GitHub.GraphQL",
    "GitHub GraphQL",
    "Performs an authorized GraphQL query against the GitHub API.",
    DisplayName = "Execute GraphQL Query")]
[UsedImplicitly]
public class ExecuteGraphQLQuery : GitHubActivity
{
    /// <summary>
    /// The GraphQL query string.
    /// </summary>
    [Input(Description = "The GraphQL query string.")]
    public Input<string> Query { get; set; } = null!;

    /// <summary>
    /// Optional variables for the GraphQL query.
    /// </summary>
    [Input(Description = "Optional variables for the GraphQL query.")]
    public Input<string?> Variables { get; set; } = null!;

    /// <summary>
    /// The query result.
    /// </summary>
    [Output(Description = "The query result as JSON.")]
    public Output<JsonElement> QueryResult { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var query = context.Get(Query)!;
        var variables = context.Get(Variables);

        var client = GetClient(context);

        // GraphQL endpoint is available through the Connection property
        var connection = client.Connection;
        
        // Prepare the GraphQL request
        var request = new
        {
            query,
            variables = !string.IsNullOrEmpty(variables) ? variables : null
        };
        
        var json = JsonSerializer.Serialize(request);
        var body = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        
        // Execute the request against the GraphQL endpoint
        var response = await connection.Post<string>(new Uri("https://api.github.com/graphql"), body, "application/json", "application/json");
        
        // Parse the response as JSON
        var jsonDocument = JsonSerializer.Deserialize<JsonDocument>(response.Body);
        context.Set(QueryResult, jsonDocument!.RootElement);
    }
}