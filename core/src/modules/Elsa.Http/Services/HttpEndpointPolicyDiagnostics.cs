using System.Text.Json;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;

namespace Elsa.Http.Services;

internal sealed class HttpEndpointPolicyDiagnostics : IWorkflowDiagnosticProvider
{
    internal const string Code = "http.endpoint.policy-without-authorization";

    public IEnumerable<WorkflowValidationError> GetDiagnostics(WorkflowGraph graph)
    {
        foreach (var endpoint in graph.Nodes.Select(node => node.Activity).OfType<HttpEndpoint>())
        {
            if (AuthorizationIsDisabled(endpoint.Authorize) && HasPolicy(endpoint.Policy))
                yield return new("This HTTP Endpoint has a Policy configured while Authorize is off. Turn on Authorize to enforce the policy.", endpoint.Id)
                {
                    Code = Code,
                    IsWarning = true
                };
        }
    }

    private static bool AuthorizationIsDisabled(Input<bool>? input) => input == null || input.Expression is { Type: "Literal" } literal &&
        (literal.Value is false or JsonElement { ValueKind: JsonValueKind.False } || bool.TryParse(ReadString(literal.Value), out var value) && !value);

    private static bool HasPolicy(Input<string?>? input) => input?.Expression switch
    {
        null => false,
        { Type: "Literal" } literal => !string.IsNullOrWhiteSpace(ReadString(literal.Value)),
        _ => true
    };

    private static string? ReadString(object? value) => value switch
    {
        string text => text,
        JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
        _ => null
    };
}
