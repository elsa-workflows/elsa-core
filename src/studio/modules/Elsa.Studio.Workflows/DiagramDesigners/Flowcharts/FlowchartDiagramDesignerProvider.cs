using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Designer.Options;
using Elsa.Studio.Workflows.UI.Contracts;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using MudBlazor;

namespace Elsa.Studio.Workflows.DiagramDesigners.Flowcharts;

/// <summary>
/// A diagram designer provider for the Flowchart designer.
/// </summary>
[UsedImplicitly]
/// <summary>
/// Provides flowchart diagram designer services.
/// </summary>
public class FlowchartDiagramDesignerProvider(ILocalizer localizer, IDialogService dialogService, IOptions<DesignerOptions> designerOptions) : IDiagramDesignerProvider
{
    /// <inheritdoc />
    public double Priority => 0;

    /// <inheritdoc />
    public bool GetSupportsActivity(JsonObject activity) => activity.GetTypeName() == "Elsa.Flowchart";

    /// <inheritdoc />
    public IDiagramDesigner GetEditor() => new FlowchartDiagramDesigner(localizer, dialogService, designerOptions);
}