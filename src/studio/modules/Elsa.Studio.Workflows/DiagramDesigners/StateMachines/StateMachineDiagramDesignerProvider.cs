using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.UI.Contracts;
using JetBrains.Annotations;

namespace Elsa.Studio.Workflows.DiagramDesigners.StateMachines;

/// <summary>
/// Provides the StateMachine diagram designer.
/// </summary>
[UsedImplicitly]
public class StateMachineDiagramDesignerProvider(ILocalizer localizer) : IDiagramDesignerProvider
{
    /// <inheritdoc />
    public double Priority => 10;

    /// <inheritdoc />
    public bool GetSupportsActivity(JsonObject activity) => activity.GetTypeName() == "Elsa.StateMachine";

    /// <inheritdoc />
    public IDiagramDesigner GetEditor() => new StateMachineDiagramDesigner(localizer);
}
