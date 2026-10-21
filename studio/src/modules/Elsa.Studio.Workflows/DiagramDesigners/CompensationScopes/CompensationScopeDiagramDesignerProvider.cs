using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;

namespace Elsa.Studio.Workflows.DiagramDesigners.CompensationScopes;

public sealed class CompensationScopeDiagramDesignerProvider(IBackendApiClientProvider backendApiClientProvider) : IDiagramDesignerProvider
{
    public double Priority => 20;

    public bool GetSupportsActivity(JsonObject activity) =>
        activity.GetTypeName() == "Elsa.CompensationScope" || IsOpaqueCompensationScope(activity);

    public IDiagramDesigner GetEditor() => new CompensationScopeDiagramDesigner(backendApiClientProvider);

    private static bool IsOpaqueCompensationScope(JsonObject activity)
    {
        if (activity.GetTypeName() != "Elsa.NotFoundActivity" ||
            activity["originalActivityJson"] is not JsonValue value ||
            !value.TryGetValue<string>(out var json) ||
            string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            return JsonNode.Parse(json) is JsonObject original && original.GetTypeName() == "Elsa.CompensationScope";
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }
}
