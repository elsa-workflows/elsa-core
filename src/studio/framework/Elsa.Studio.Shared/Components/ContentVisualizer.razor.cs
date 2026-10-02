using BlazorMonaco.Editor;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Elsa.Studio.Visualizers;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Elsa.Studio.Components;

/// <summary>
/// Represents the content visualizer.
/// </summary>
public partial class ContentVisualizer : ComponentBase
{
    private int _tabIndex;
    private string _selectedItem = "";
    private string? _pretty;
    private TabulatedContentVisualizer? _table;
    private IContentVisualizer _selectedVisualizer = new DefaultContentVisualizer();
    private List<IContentVisualizer> _availableVisualizers = new();
    private readonly string _monacoEditorId = $"monaco-editor-{Guid.NewGuid():N}";
    private StandaloneCodeEditor? _monacoEditor;

    /// <summary>
    /// Indicates whether the editor is in read-only mode.
    /// </summary>
    public bool IsReadOnly
    {
        get;
        set
        {
            field = value;
            _monacoEditor?.UpdateOptions(
                new()
                {
                    ReadOnly = value
                });
        }
    } = true;

    /// <summary>
    /// Provides the selected item.
    /// </summary>
    public string SelectedItem
    {
        get => _selectedItem;
        set
        {
            _selectedItem = value;
            _ = OnFormatterChanged();
        }
    }

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    /// <summary>
    /// Gets or sets the DataPanelItem associated with the ContentVisualizer. This property holds the
    /// data panel item which includes information like label, text, link, and optional actions that
    /// can be visualized using a content visualizer.
    /// </summary>
    [Parameter] public DataPanelItem DataPanelItem { get; set; } = null!;

    [Inject] private ILocalizer Localizer { get; set; } = null!;
    [Inject] private IContentVisualizerProvider VisualizationProvider { get; set; } = null!;
    [Inject] private IClipboard Clipboard { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;

    /// <summary>
    /// Performs the on initialized operation.
    /// </summary>
    protected override void OnInitialized()
    {
        _availableVisualizers = VisualizationProvider.GetAll().ToList();
        if (DataPanelItem.Text != null) 
            _selectedVisualizer = VisualizationProvider.MatchOrDefault(DataPanelItem.Text);
        _selectedItem = _selectedVisualizer.Name;
        FormatUsing(_selectedVisualizer);
    }

    private StandaloneEditorConstructionOptions ConfigureMonacoEditor(StandaloneCodeEditor editor)
    {
        return new()
        {
            Language = _selectedVisualizer.Syntax,
            Value = _pretty,
            FontFamily = "Roboto Mono, monospace",
            RenderLineHighlight = "none",
            Minimap = new()
            {
                Enabled = false
            },
            AutomaticLayout = true,
            LineNumbers = "on",
            Theme = "vs",
            RoundedSelection = true,
            ScrollBeyondLastLine = false,
            OverviewRulerLanes = 0,
            OverviewRulerBorder = false,
            LineDecorationsWidth = 0,
            HideCursorInOverviewRuler = true,
            GlyphMargin = false,
            ReadOnly = IsReadOnly,
            DomReadOnly = IsReadOnly
        };
    }

    private async Task OnFormatterChanged()
    {
        var visualizer = _availableVisualizers.FirstOrDefault(f => f.Name == SelectedItem);
        if (visualizer != null)
        {
            FormatUsing(visualizer);

            var model = await _monacoEditor!.GetModel();
            await Global.SetModelLanguage(JSRuntime, model, _selectedVisualizer.Syntax);
        }
    }

    private void FormatUsing(IContentVisualizer visualizer)
    {
        _selectedVisualizer = visualizer;
        
        if (DataPanelItem.Text != null)
        {
            _pretty = visualizer.ToPretty(DataPanelItem.Text);
            _table = visualizer.ToTable(DataPanelItem.Text);
        }

        _tabIndex = 0;
        StateHasChanged();
    }

    private async Task OnCopyClicked()
    {
        await Clipboard.CopyText(_pretty ?? DataPanelItem.Text ?? string.Empty);
        Snackbar.Add($"{DataPanelItem.Label} copied", Severity.Success);
    }

    private void OnClosedClicked()
    {
        MudDialog.Cancel();
    }
}