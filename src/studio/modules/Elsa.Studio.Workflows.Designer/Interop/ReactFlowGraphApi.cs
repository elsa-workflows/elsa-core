using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Domain.Models;
using Microsoft.JSInterop;

namespace Elsa.Studio.Workflows.Designer.Interop;

/// <summary>
/// Wraps the React Flow JS module, mirroring the call surface of <see cref="X6GraphApi"/>.
/// </summary>
public class ReactFlowGraphApi
{
    private readonly IJSObjectReference _module;
    private readonly string _containerId;

    /// <summary>Initializes the API for a given container.</summary>
    public ReactFlowGraphApi(IJSObjectReference module, string containerId)
    {
        _module = module;
        _containerId = containerId;
    }

    /// <summary>Reads the current graph state in the same shape as the X6 path
    /// (a `cells` array discriminated by `shape`) so the C# side can reuse the
    /// existing X6Graph deserialization.</summary>
    public async Task<JsonElement> ReadGraphAsync() =>
        await _module.InvokeAsync<JsonElement>("readReactGraph", _containerId);

    /// <summary>Disposes the React tree and removes the binding.</summary>
    public async Task DisposeGraphAsync()
    {
        try
        {
            await _module.InvokeVoidAsync("disposeReactGraph", _containerId);
        }
        catch (JSDisconnectedException)
        {
            // The browser already tore the page down; nothing to do.
        }
    }

    /// <summary>Loads a graph (nodes + edges) into the designer.</summary>
    public async Task LoadGraphAsync(X6Graph graph)
    {
        var serialized = SerializeGraph(graph);
        await _module.InvokeVoidAsync("loadReactGraph", _containerId, serialized);
    }

    /// <summary>Centers and fits the canvas to its content.</summary>
    public async Task ZoomToFitAsync() => await _module.InvokeVoidAsync("zoomReactGraphToFit", _containerId);

    /// <summary>Centers the canvas content.</summary>
    public async Task CenterContentAsync() => await _module.InvokeVoidAsync("centerReactGraphContent", _containerId);

    /// <summary>Selects an activity by ID.</summary>
    public async Task SelectActivityAsync(string id) => await _module.InvokeVoidAsync("selectReactActivity", _containerId, id);

    /// <summary>Adds an activity node to the designer.</summary>
    public async Task AddActivityNodeAsync(X6ActivityNode node)
    {
        var element = JsonSerializer.SerializeToElement(node, GetSerializerOptions());
        await _module.InvokeVoidAsync("addReactActivityNode", _containerId, element);
    }

    /// <summary>Updates an existing activity node (data, ports, size).</summary>
    public async Task UpdateActivityAsync(X6ActivityNode node)
    {
        var element = JsonSerializer.SerializeToElement(node, GetSerializerOptions());
        await _module.InvokeVoidAsync("updateReactActivity", _containerId, element);
    }

    /// <summary>Updates the live activity stats badge for a node.</summary>
    public async Task UpdateActivityStatsAsync(string activityId, ActivityStats stats) =>
        await _module.InvokeVoidAsync("updateReactActivityStats", _containerId, activityId, stats);

    /// <summary>Re-runs the dagre layout on the current React Flow nodes/edges.</summary>
    public async Task AutoLayoutAsync() =>
        await _module.InvokeVoidAsync("autoLayoutReactGraph", _containerId);

    /// <summary>Sets the Sequence layout orientation.</summary>
    public async Task SetSequenceOrientationAsync(string orientation) =>
        await _module.InvokeVoidAsync("setSequenceOrientation", _containerId, orientation);

    /// <summary>Moves the selected Sequence activity earlier or later.</summary>
    public async Task MoveSelectedSequenceNodeAsync(int direction) =>
        await _module.InvokeVoidAsync("moveSelectedReactSequenceNode", _containerId, direction);

    /// <summary>Adds the specified activity nodes and edges (after .NET regenerated their IDs).</summary>
    public async Task PasteCellsAsync(IEnumerable<X6ActivityNode> activityNodes, X6Edge[] edges)
    {
        var options = GetSerializerOptions();
        var nodeElements = JsonSerializer.SerializeToElement(activityNodes, options);
        var edgeElements = JsonSerializer.SerializeToElement(edges, options);
        await _module.InvokeVoidAsync("pasteReactCells", _containerId, nodeElements, edgeElements);
    }

    private static JsonSerializerOptions GetSerializerOptions()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static string SerializeGraph(X6Graph graph) =>
        JsonSerializer.Serialize(graph, GetSerializerOptions());
}
