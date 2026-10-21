using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.UI.Contracts;

namespace Elsa.Studio.Workflows.DiagramDesigners.CompensationScopes;

public sealed class CompensationScopeDiagramDesignerProvider : IDiagramDesignerProvider
{
    public double Priority => 20;

    public bool GetSupportsActivity(JsonObject activity) =>
        activity.GetTypeName() == "Elsa.CompensationScope" || NativeCompensationDraftCapability.IsOpaqueCompensationScope(activity);

    public IDiagramDesigner GetEditor() => new CompensationScopeDiagramDesigner();
}
