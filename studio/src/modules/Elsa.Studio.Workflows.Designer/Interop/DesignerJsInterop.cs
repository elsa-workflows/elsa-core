using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Designer.Components;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Designer.Options;
using Elsa.Studio.Workflows.Domain.Models;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Elsa.Studio.Workflows.Designer.Interop;

/// <summary>
/// Provides access to the designer JavaScript module.
/// </summary>
public class DesignerJsInterop(
    IJSRuntime jsRuntime,
    IOptions<DesignerOptions> options,
    IServiceProvider serviceProvider,
    IThemeService themeService) : JsInteropBase(jsRuntime)
{
    /// <summary>
    /// Provides the string.
    /// </summary>
    protected override string ModuleName => "designer";

    /// <summary>
    /// Creates a new X6 graph object and returns its ID.
    /// </summary>
    /// <param name="containerId">The ID of the container element.</param>
    /// <param name="componentRef">A reference to the <see cref="FlowchartDesigner"/> component.</param>
    /// <param name="isReadOnly">Whether the graph is read-only.</param>
    /// <returns>The ID of the graph.</returns>
    public ValueTask<X6GraphApi> CreateGraphAsync(string containerId, DotNetObjectReference<FlowchartDesigner> componentRef, bool isReadOnly = false) =>
        CreateGraphAsync<FlowchartDesigner>(containerId, componentRef, isReadOnly);

    /// <summary>
    /// Creates a new X6 graph object and returns its API wrapper.
    /// </summary>
    public async ValueTask<X6GraphApi> CreateGraphAsync<TComponent>(string containerId, DotNetObjectReference<TComponent> componentRef, bool isReadOnly = false, string mode = "flowchart")
        where TComponent : class
    {
        return await TryInvokeAsync(async module =>
        {
            var graphSettings = options.Value.GraphSettings;
            var settings = new
            {
                graphSettings.Grid,
                graphSettings.MagnetThreshold,
                graphSettings.Panning,
                graphSettings.Mousewheel,
                graphSettings.ResizingEnabled,
                Mode = mode,
                Theme = X6DesignerTheme.FromPalette(themeService.CurrentPalette)
            };

            await module.InvokeAsync<string>("createGraph", containerId, componentRef, isReadOnly, settings);
            return new X6GraphApi(module, serviceProvider, containerId);
        });
    }

    /// <summary>
    /// Creates a new read-only BPMN graph and returns its API wrapper. The graph id is the container id.
    /// </summary>
    /// <param name="containerId">The ID of the container element.</param>
    /// <param name="componentRef">A reference to the <see cref="BpmnDesigner"/> component.</param>
    public async ValueTask<BpmnGraphApi> CreateBpmnGraphAsync(string containerId, DotNetObjectReference<BpmnDesigner> componentRef)
    {
        return await TryInvokeAsync(async module =>
        {
            var graphId = await module.InvokeAsync<string>("createBpmnGraph", containerId, componentRef);
            return new BpmnGraphApi(module, graphId);
        });
    }

    /// <summary>
    /// Provides the task.
    /// </summary>
    public async Task UpdateActivitySizeAsync(string elementId, JsonObject activity, Elsa.Api.Client.Shared.Models.Size? size = null, int? portCount = null)
    {
        var serializerOptions = GetSerializerOptions();
        var activityJson = JsonSerializer.Serialize(activity, serializerOptions);
        await TryInvokeAsync(module => module.InvokeVoidAsync("updateActivitySize", elementId, activityJson, size, portCount));
    }

    /// <summary>
    /// Provides the task.
    /// </summary>
    public async Task UpdateActivityStatsAsync(string elementId, string activityId, ActivityStats stats) =>
        await TryInvokeAsync(module => module.InvokeVoidAsync("updateActivityStats", elementId, activityId, stats));

    /// <summary>
    /// Performs the raise activity selected operation asynchronously.
    /// </summary>
    /// <param name="elementId">The element id.</param>
    /// <param name="activity">The activity.</param>
    public async Task RaiseActivitySelectedAsync(string elementId, JsonObject activity)
    {
        var serializerOptions = GetSerializerOptions();
        var activityJson = JsonSerializer.Serialize(activity, serializerOptions);
        await TryInvokeAsync(module => module.InvokeVoidAsync("raiseActivitySelected", elementId, activityJson));
    }

    /// <summary>
    /// Performs the raise activity embedded port selected operation asynchronously.
    /// </summary>
    /// <param name="elementId">The element id.</param>
    /// <param name="activity">The activity.</param>
    /// <param name="portName">The port name.</param>
    public async Task RaiseActivityEmbeddedPortSelectedAsync(string elementId, JsonObject activity, string portName)
    {
        var serializerOptions = GetSerializerOptions();
        var activityJson = JsonSerializer.Serialize(activity, serializerOptions);
        await TryInvokeAsync(module => module.InvokeVoidAsync("raiseActivityEmbeddedPortSelected", elementId, activityJson, portName));
    }

    private static JsonSerializerOptions GetSerializerOptions()
    {
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        serializerOptions.Converters.Add(new JsonStringEnumConverter());
        return serializerOptions;
    }
}
