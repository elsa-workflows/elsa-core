using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.UI.Contracts;
using JetBrains.Annotations;

namespace Elsa.Studio.Workflows.DiagramDesigners.Sequences;

/// <summary>
/// Provides Sequence diagram designer services.
/// </summary>
[UsedImplicitly]
public class SequenceDiagramDesignerProvider(ILocalizer localizer) : IDiagramDesignerProvider
{
    /// <inheritdoc />
    public double Priority => 10;

    /// <inheritdoc />
    public bool GetSupportsActivity(JsonObject activity) => activity.GetTypeName() == "Elsa.Sequence";

    /// <inheritdoc />
    public IDiagramDesigner GetEditor() => new SequenceDiagramDesigner(localizer);
}
