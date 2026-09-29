using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Elsa.Studio.Workflows.Designer.Contracts;
using Elsa.Studio.Workflows.Designer.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Elsa.Studio.Workflows.Designer.Interop;

/// Provides a wrapper around the X6 graph API.
public class X6GraphApi
{
    private static readonly JsonSerializerOptions ExportSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly IJSObjectReference _module;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _containerId;

    /// <summary>
    /// Initializes a new instance of the <see cref="X6GraphApi"/> class.
    /// </summary>
    /// <param name="module">The JavaScript module reference.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="containerId">The ID of the container element.</param>
    public X6GraphApi(IJSObjectReference module, IServiceProvider serviceProvider, string containerId)
    {
        _module = module;
        _serviceProvider = serviceProvider;
        _containerId = containerId;
    }

    /// <summary>
    /// Reads the flowchart from the graph.
    /// </summary>
    /// <returns>The flowchart.</returns>
    public async Task<JsonElement> ReadGraphAsync() => await InvokeAsync(module => module.InvokeAsync<JsonElement>("readGraph", _containerId));

    /// Disposes the graph.
    public async Task DisposeGraphAsync() => await TryInvokeAsync(module => module.InvokeVoidAsync("disposeGraph", _containerId));

    /// <summary>
    /// Applies the active Elsa theme to the X6 graph.
    /// </summary>
    public async Task ApplyThemeAsync(X6DesignerTheme theme) =>
        await InvokeAsync(module => module.InvokeVoidAsync("applyGraphTheme", _containerId, theme));

    /// <summary>
    /// Adds a node to the graph.
    /// </summary>
    /// <param name="node">The node.</param>
    public async Task AddActivityNodeAsync(X6ActivityNode node)
    {
        var serializerOptions = GetSerializerOptions();
        var nodeElement = JsonSerializer.SerializeToElement(node, serializerOptions);

        await InvokeAsync(module => module.InvokeVoidAsync("addActivityNode", _containerId, nodeElement));
    }

    /// <summary>
    /// Selects the specified activity in the graph.
    /// </summary>
    /// <param name="id">The ID of the activity to select.</param>
    public async Task SelectActivityAsync(string id)
    {
        await InvokeAsync(module => module.InvokeVoidAsync("selectActivity", _containerId, id));
    }

    /// <summary>
    /// Adds the specified activity nodes and edges to the graph.
    /// </summary>
    /// <param name="activityNodes">The activity nodes.</param>
    /// <param name="edges">The edges.</param>
    public async Task PasteCellsAsync(IEnumerable<X6ActivityNode> activityNodes, X6Edge[] edges)
    {
        var serializerOptions = GetSerializerOptions();
        var activityNodeElements = JsonSerializer.SerializeToElement(activityNodes, serializerOptions);
        var edgeElements = JsonSerializer.SerializeToElement(edges, serializerOptions);
        await InvokeAsync(module => module.InvokeVoidAsync("pasteCells", _containerId, activityNodeElements, edgeElements));
    }

    /// <summary>
    /// Loads the specified model into the graph.
    /// </summary>
    /// <param name="graph">The model.</param>
    public async Task LoadGraphAsync(X6Graph graph)
    {
        var serializedGraph = SerializeGraph(graph);
        await InvokeAsync(module => module.InvokeVoidAsync("loadGraph", _containerId, serializedGraph));
    }

    /// <summary>
    /// Loads a renderer-specific X6 graph projection.
    /// </summary>
    public async Task LoadGraphAsync<TGraph>(TGraph graph)
    {
        var serializedGraph = SerializeGraph(graph);
        await InvokeAsync(module => module.InvokeVoidAsync("loadGraph", _containerId, serializedGraph));
    }

    /// <summary>
    /// Selects a native X6 cell and optionally centers it.
    /// </summary>
    public async Task SelectCellAsync(string id, bool center = false) =>
        await InvokeAsync(module => module.InvokeVoidAsync("selectCell", _containerId, id, center));

    /// Zoom the canvas to fit the content.
    public async Task ZoomToFitAsync() => await InvokeAsync(module => module.InvokeVoidAsync("zoomToFit", _containerId));

    /// Center the canvas content.
    public async Task CenterContentAsync() => await InvokeAsync(module => module.InvokeVoidAsync("centerContent", _containerId));

    /// <summary>
    /// Exports the canvas content as an image and lets the browser download it.
    /// </summary>
    /// <param name="options">The export options.</param>
    public async Task ExportGraphAsync(ExportGraphOptions options)
    {
        var payload = CreateExportPayload(options);
        await InvokeAsync(module => module.InvokeVoidAsync("exportGraph", _containerId, payload));
    }

    /// <summary>
    /// Serializes the export options into the payload the <c>exportGraph</c> JavaScript function expects. That
    /// function switches on lowercase format names ("png", "jpeg", "svg") and throws on anything else, so the
    /// enum must be written as a camel-cased string rather than as its member name or its numeric value.
    /// </summary>
    internal static JsonElement CreateExportPayload(ExportGraphOptions options) => JsonSerializer.SerializeToElement(options, ExportSerializerOptions);

    /// Adjusts the graph layout.
    public async Task AutoLayoutAsync(X6Graph graph)
    {
        var serializedGraph = SerializeGraph(graph);
        await InvokeAsync(module => module.InvokeVoidAsync("autoLayout", _containerId, serializedGraph));
    }

    /// <summary>
    /// Applies the shared X6 auto-layout to a renderer-specific graph projection.
    /// </summary>
    public async Task AutoLayoutAsync<TGraph>(TGraph graph)
    {
        var serializedGraph = SerializeGraph(graph);
        await InvokeAsync(module => module.InvokeVoidAsync("autoLayout", _containerId, serializedGraph));
    }

    /// <summary>
    /// Sets the Sequence layout orientation on constrained graph designers.
    /// </summary>
    public async Task SetSequenceOrientationAsync(string orientation) =>
        await InvokeAsync(module => module.InvokeVoidAsync("setX6SequenceOrientation", _containerId, orientation));

    /// <summary>
    /// Moves the selected Sequence activity earlier or later.
    /// </summary>
    public async Task MoveSelectedSequenceNodeAsync(int direction) =>
        await InvokeAsync(module => module.InvokeVoidAsync("moveSelectedX6SequenceNode", _containerId, direction));

    /// <summary>
    /// Updates the node with the specified activity. 
    /// </summary>
    /// <param name="activity">The activity.</param>
    public async Task UpdateActivityAsync(JsonObject activity)
    {
        var serializerOptions = GetSerializerOptions();
        var mapperFactory = _serviceProvider.GetRequiredService<IMapperFactory>();
        var activityMapper = await mapperFactory.CreateActivityMapperAsync();
        var ports = activityMapper.GetPorts(activity);
        var activityElement = JsonSerializer.SerializeToElement(activity, serializerOptions);
        await InvokeAsync(module => module.InvokeVoidAsync("updateActivity", _containerId, activityElement, ports));
    }

    private async Task InvokeAsync(Func<IJSObjectReference, ValueTask> func) => await func(_module);

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

    private async Task<T> InvokeAsync<T>(Func<IJSObjectReference, ValueTask<T>> func) => await func(_module);

    // Serializing the graph here instead of relying on the JS interop layer to avoid the max depth of 32 exception.
    private static string SerializeGraph<TGraph>(TGraph graph)
    {
        var options = GetSerializerOptions();
        return JsonSerializer.Serialize(graph, options);
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
