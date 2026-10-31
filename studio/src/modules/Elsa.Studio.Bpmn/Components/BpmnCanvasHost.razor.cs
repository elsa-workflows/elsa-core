using System.Text.Json;
using Elsa.Studio.Bpmn.Designers;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Elsa.Studio.Bpmn.Components;

/// <summary>
/// Draws a BPMN document with the designer <see cref="Designer"/> names, through JS interop with that designer's module.
/// </summary>
/// <remarks>
/// <para>
/// The designer is created on the first render and lives as long as the component; changing <see cref="Designer"/> or
/// <see cref="ReadOnly"/> afterwards has no effect, so a caller that needs another one re-creates the component, for
/// example with a new <c>@key</c>. Its methods may be called as soon as the component exists: they wait until the
/// designer has been created. When it cannot be created, the component shows why and every method throws.
/// </para>
/// <para>
/// The component does no saving of its own: the caller loads XML, saves it, and decides when a save that
/// <see cref="BpmnSaveGate"/> holds back may go ahead.
/// </para>
/// </remarks>
public partial class BpmnCanvasHost : IAsyncDisposable
{
    internal const string HostModulePath = "./_content/Elsa.Studio.Bpmn/bpmn-canvas-host.js";

    private readonly TaskCompletionSource<IJSObjectReference> _designerReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private ElementReference _canvas;
    private IJSObjectReference? _hostModule;
    private IJSObjectReference? _designer;
    private DotNetObjectReference<BpmnCanvasHost>? _listener;
    private string? _failure;
    private bool _disposed;

    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;

    /// <summary>The designer to draw with, as <see cref="BpmnDesignerResolver"/> chose it.</summary>
    [Parameter, EditorRequired] public BpmnDesignerDescriptor Designer { get; set; } = null!;

    /// <summary>Whether the user may change the document. Overlays and selection work either way.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Raised when the document or its undo state changes.</summary>
    [Parameter] public EventCallback<BpmnDesignerChange> Changed { get; set; }

    /// <summary>Raised when the selection changes.</summary>
    [Parameter] public EventCallback<BpmnDesignerSelection> SelectionChanged { get; set; }

    /// <summary>CSS classes for the component's root element, which the canvas fills.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Inline styles for the component's root element, which the canvas fills.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Shows <paramref name="xml"/>. When the designer cannot show it, it keeps the document it had.</summary>
    public async Task<BpmnDesignerLoadResult> LoadAsync(string xml) =>
        await (await GetDesignerAsync()).InvokeAsync<BpmnDesignerLoadResult>("load", xml);

    /// <summary>The document as the designer would save it now. Throws when no document has been loaded.</summary>
    public async Task<string> SaveAsync() =>
        (await (await GetDesignerAsync()).InvokeAsync<JsonElement>("save")).GetProperty("xml").GetString()!;

    /// <summary>Selects the element with <paramref name="elementId"/>, or clears the selection when it is null. Returns whether the element exists.</summary>
    public async Task<bool> SelectAsync(string? elementId) =>
        await (await GetDesignerAsync()).InvokeAsync<bool>("select", elementId);

    /// <summary>The element's activity binding, or null when it has none or does not exist.</summary>
    public async Task<BpmnDesignerBinding?> GetBindingAsync(string elementId) =>
        await (await GetDesignerAsync()).InvokeAsync<BpmnDesignerBinding?>("getBinding", elementId);

    /// <summary>Sets the element's activity binding, or removes it when <paramref name="binding"/> is null, as one undo step.</summary>
    public async Task<BpmnDesignerSetBindingResult> SetBindingAsync(string elementId, BpmnDesignerBinding? binding) =>
        await (await GetDesignerAsync()).InvokeAsync<BpmnDesignerSetBindingResult>("setBinding", elementId, binding);

    /// <summary>Undoes the last edit. Returns whether there was one.</summary>
    public async Task<bool> UndoAsync() => await (await GetDesignerAsync()).InvokeAsync<bool>("undo");

    /// <summary>Redoes the last undone edit. Returns whether there was one.</summary>
    public async Task<bool> RedoAsync() => await (await GetDesignerAsync()).InvokeAsync<bool>("redo");

    /// <summary>Shows each element's instance state, replacing the overlays shown before.</summary>
    public async Task<BpmnDesignerOverlayResult> ApplyOverlaysAsync(IReadOnlyDictionary<string, BpmnDesignerElementStats>? statsById) =>
        await (await GetDesignerAsync()).InvokeAsync<BpmnDesignerOverlayResult>("applyOverlays", statsById);

    /// <summary>Removes every overlay.</summary>
    public async Task<BpmnDesignerOverlayResult> ClearOverlaysAsync() =>
        await (await GetDesignerAsync()).InvokeAsync<BpmnDesignerOverlayResult>("clearOverlays");

    /// <summary>
    /// Marks each element a problem is about, replacing the markers shown before; <see langword="null"/> or an empty list
    /// clears them. Returns <see langword="null"/> when the designer has no problem markers, which the contract leaves
    /// optional.
    /// </summary>
    public async Task<BpmnDesignerOverlayResult?> ApplyMarkersAsync(IReadOnlyList<BpmnDesignerMarker>? markers) =>
        await (await GetDesignerAsync()).InvokeAsync<BpmnDesignerOverlayResult?>("applyMarkers", markers);

    /// <summary>Removes every problem marker. Returns <see langword="null"/> when the designer has no problem markers.</summary>
    public async Task<BpmnDesignerOverlayResult?> ClearMarkersAsync() =>
        await (await GetDesignerAsync()).InvokeAsync<BpmnDesignerOverlayResult?>("clearMarkers");

    /// <summary>Called by the designer when the document or its undo state changes.</summary>
    [JSInvokable]
    public Task OnDesignerChanged(BpmnDesignerChange change) => Changed.InvokeAsync(change);

    /// <summary>Called by the designer when the selection changes.</summary>
    [JSInvokable]
    public Task OnDesignerSelectionChanged(BpmnDesignerSelection selection) => SelectionChanged.InvokeAsync(selection);

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        try
        {
            _listener = DotNetObjectReference.Create(this);
            _hostModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", HostModulePath);
            _designer = await _hostModule.InvokeAsync<IJSObjectReference>("createDesigner", Designer.DesignerId, Designer.ModuleUrl, Designer.StylesheetUrl, _canvas, ReadOnly, _listener);
        }
        catch (Exception) when (_disposed)
        {
            // Disposed while the designer was being created: its references are already released.
            await DisposeDesignerAsync();
            return;
        }
        catch (Exception exception)
        {
            _failure = $"The BPMN designer '{Designer?.DesignerId}' could not be loaded: {exception.Message}";
            _designerReady.TrySetException(new InvalidOperationException(_failure, exception));
            StateHasChanged();
            return;
        }

        if (_disposed)
        {
            await DisposeDesignerAsync();
            return;
        }

        _designerReady.TrySetResult(_designer);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        _designerReady.TrySetCanceled();
        await DisposeDesignerAsync();
        GC.SuppressFinalize(this);
    }

    private Task<IJSObjectReference> GetDesignerAsync() =>
        _disposed ? throw new ObjectDisposedException(nameof(BpmnCanvasHost)) : _designerReady.Task;

    private async Task DisposeDesignerAsync()
    {
        // Disposal and a creation that finishes after it can both get here, interleaved at their awaits. Each takes the
        // references there are now and clears them before it awaits, so each releases only what it took, and once.
        var designer = _designer;
        var hostModule = _hostModule;
        using var listener = _listener;
        _designer = null;
        _hostModule = null;
        _listener = null;

        try
        {
            if (designer is not null)
            {
                await designer.InvokeVoidAsync("dispose");
                await designer.DisposeAsync();
            }

            if (hostModule is not null)
                await hostModule.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the browser's state with it.
        }
    }
}
