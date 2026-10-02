using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Microsoft.JSInterop;

namespace Elsa.Studio.Workflows.Designer.Interop;

/// <summary>
/// Provides a wrapper around the BPMN canvas's JS interop surface (<c>src/designer/api/bpmn-designer.ts</c>).
/// The graph id a BPMN canvas is registered under is its container id, the same convention the X6
/// flowchart canvas uses, which is why zooming and centering below reuse the very same <c>zoomToFit</c>
/// and <c>centerContent</c> functions <see cref="X6GraphApi"/> calls.
/// </summary>
public class BpmnGraphApi
{
    private readonly IJSObjectReference _module;
    private readonly string _graphId;

    /// <summary>
    /// Initializes a new instance of the <see cref="BpmnGraphApi"/> class.
    /// </summary>
    /// <param name="module">The JavaScript module reference.</param>
    /// <param name="graphId">The id of the graph, which is the id of its container element.</param>
    public BpmnGraphApi(IJSObjectReference module, string graphId)
    {
        _module = module;
        _graphId = graphId;
    }

    /// <summary>
    /// Loads the specified BPMN diagram input into the graph, replacing whatever it held.
    /// </summary>
    /// <param name="input">The <c>{ activity, sourceXml, activityDescriptors }</c> payload the view model builder reads.</param>
    /// <returns>The view model's diagnostics, in document order.</returns>
    public async Task<IReadOnlyList<BpmnDiagnostic>> LoadDiagramAsync(JsonObject input)
    {
        var inputElement = JsonSerializer.SerializeToElement(input, GetSerializerOptions());
        var diagnostics = await InvokeAsync(module => module.InvokeAsync<BpmnDiagnostic[]>("loadBpmnDiagram", _graphId, inputElement));
        return diagnostics;
    }

    /// <summary>
    /// Updates the stats of every element bound to the specified activity.
    /// </summary>
    /// <param name="activityId">The Elsa activity id.</param>
    /// <param name="stats">The stats to apply, or null to clear them.</param>
    public async Task UpdateActivityStatsAsync(string activityId, ActivityStats? stats) =>
        await InvokeAsync(module => module.InvokeVoidAsync("updateBpmnActivityStats", _graphId, activityId, stats));

    /// <summary>
    /// Replaces the whole element-keyed instance overlay: instance state for a gateway, an intermediate event or a
    /// sequence flow, keyed by BPMN element id (or, for a flow, by flow id).
    /// </summary>
    /// <param name="elementStats">The stats to apply, or null (or empty) to clear the overlay entirely.</param>
    public async Task UpdateElementStatsAsync(IReadOnlyDictionary<string, BpmnElementStats>? elementStats) =>
        await InvokeAsync(module => module.InvokeVoidAsync("updateBpmnElementStats", _graphId, elementStats));

    /// <summary>
    /// Selects the specified element, optionally centering the viewport on it.
    /// </summary>
    /// <param name="elementId">The BPMN element id.</param>
    /// <param name="center">Whether to center the viewport on the element.</param>
    public async Task SelectElementAsync(string elementId, bool center = false) =>
        await InvokeAsync(module => module.InvokeVoidAsync("selectBpmnElement", _graphId, elementId, center));

    /// Zoom the canvas to fit the content.
    public async Task ZoomToFitAsync() => await InvokeAsync(module => module.InvokeVoidAsync("zoomToFit", _graphId));

    /// Center the canvas content.
    public async Task CenterContentAsync() => await InvokeAsync(module => module.InvokeVoidAsync("centerContent", _graphId));

    /// Disposes the graph.
    public async Task DisposeGraphAsync() => await TryInvokeAsync(module => module.InvokeVoidAsync("disposeBpmnGraph", _graphId));

    private async Task InvokeAsync(Func<IJSObjectReference, ValueTask> func) => await func(_module);
    private async Task<T> InvokeAsync<T>(Func<IJSObjectReference, ValueTask<T>> func) => await func(_module);

    private async Task TryInvokeAsync(Func<IJSObjectReference, ValueTask> func)
    {
        try
        {
            await func(_module);
        }
        catch (JSDisconnectedException)
        {
            // Ignore.
        }
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
