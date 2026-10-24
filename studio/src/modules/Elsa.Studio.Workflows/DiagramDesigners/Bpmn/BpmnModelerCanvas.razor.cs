using Elsa.Studio.Bpmn.Components;
using Elsa.Studio.Bpmn.Designers;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.DiagramDesigners.Bpmn;

/// <summary>
/// The enabled BPMN designer, editing <see cref="Session"/>'s document: it loads every revision the session reads,
/// and reports the designer's changes and selection back to it.
/// </summary>
public partial class BpmnModelerCanvas : IBpmnModelerCanvas, IDisposable
{
    private BpmnCanvasHost _host = null!;
    private BpmnModelerSession? _attachedSession;
    private int _loadedVersion;
    private bool _isLoading;
    private bool _rendered;

    /// <summary>The document this canvas edits.</summary>
    [Parameter, EditorRequired] public BpmnModelerSession Session { get; set; } = null!;

    [Inject] private BpmnDesignerResolver DesignerResolver { get; set; } = null!;

    /// <summary>
    /// The designer to show. The editor only opens a <see cref="BpmnModelerSession"/> when the resolver has one, so it is
    /// never null here.
    /// </summary>
    private BpmnDesignerDescriptor Designer => DesignerResolver.Resolve()!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_attachedSession, Session))
            return;

        Detach();
        _attachedSession = Session;
        _loadedVersion = 0;
        Session.Changed += OnSessionChanged;
        Session.AttachCanvas(this);

        if (_rendered)
            _ = InvokeAsync(OpenAsync);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        _rendered = true;
        await OpenAsync();
    }

    /// <summary>Reads the session's document, unless it already has, and loads it into the designer.</summary>
    private async Task OpenAsync()
    {
        await Session.EnsureLoadedAsync();
        await LoadCurrentDocumentAsync();
    }

    /// <summary>
    /// Loads the session's document into the designer when it holds a revision the designer has not shown yet. One load
    /// runs at a time; a revision read while one runs is loaded when it finishes.
    /// </summary>
    private async Task LoadCurrentDocumentAsync()
    {
        var session = Session;

        while (!_isLoading && session.Xml is { } xml && session.DocumentVersion != _loadedVersion && ReferenceEquals(session, _attachedSession))
        {
            _isLoading = true;
            _loadedVersion = session.DocumentVersion;

            try
            {
                session.CanvasLoaded(await _host.LoadAsync(xml), _loadedVersion);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // The host shows why the designer could not be created; the session records that nothing was loaded.
                session.CanvasLoaded(new(false, exception.Message, []), _loadedVersion);
            }
            finally
            {
                _isLoading = false;
            }
        }
    }

    private void OnSessionChanged()
    {
        if (_rendered)
            _ = InvokeAsync(LoadCurrentDocumentAsync);
    }

    private void OnChanged(BpmnDesignerChange change) => Session.CanvasChanged(change);

    private void OnSelectionChanged(BpmnDesignerSelection selection) => Session.CanvasSelectionChanged(selection);

    Task<string> IBpmnModelerCanvas.SaveAsync() => _host.SaveAsync();

    Task IBpmnModelerCanvas.UndoAsync() => _host.UndoAsync();

    Task IBpmnModelerCanvas.RedoAsync() => _host.RedoAsync();

    Task<BpmnDesignerBinding?> IBpmnModelerCanvas.GetBindingAsync(string elementId) => _host.GetBindingAsync(elementId);

    Task<BpmnDesignerSetBindingResult> IBpmnModelerCanvas.SetBindingAsync(string elementId, BpmnDesignerBinding binding) => _host.SetBindingAsync(elementId, binding);

    Task<bool> IBpmnModelerCanvas.SelectAsync(string elementId) => _host.SelectAsync(elementId);

    Task IBpmnModelerCanvas.ApplyMarkersAsync(IReadOnlyList<BpmnDesignerMarker> markers) => _host.ApplyMarkersAsync(markers);

    private void Detach()
    {
        if (_attachedSession == null)
            return;

        _attachedSession.Changed -= OnSessionChanged;
        _attachedSession.DetachCanvas(this);
        _attachedSession = null;
    }

    /// <inheritdoc />
    public void Dispose() => Detach();
}
