using BlazorMonaco.Editor;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Elsa.Studio.Components;

/// <summary>
/// Represents a dialog for editing code with Monaco.
/// </summary>
public partial class CodeEditorDialog : IDisposable
{
    private readonly string _monacoEditorId = $"monaco-editor-{Guid.NewGuid():N}";
    private StandaloneCodeEditor? _monacoEditor;
    private bool _isInternalContentChange;
    private string? _lastMonacoEditorContent;
    private string? _validationError;

    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    /// <summary>
    /// Gets or sets the current code content displayed and edited in the Monaco editor dialog.
    /// </summary>
    [Parameter] public string Value { get; set; } = null!;

    /// <summary>
    /// Gets or sets the label for the code editor dialog.
    /// </summary>
    [Parameter] public string Label { get; set; } = null!;

    /// <summary>
    /// Gets or sets the helper text for the code editor dialog.
    /// This text provides additional information or guidance related to the code being edited.
    /// </summary>
    [Parameter] public string HelperText { get; set; } = null!;

    /// <summary>
    /// Gets or sets the label for the language selection in the code editor dialog.
    /// </summary>
    [Parameter] public string LanguageLabel { get; set; } = null!;

    /// <summary>
    /// Gets or sets the programming language syntax used by the Monaco code editor within the dialog.
    /// </summary>
    [Parameter] public string MonacoLanguage { get; set; } = null!;

    /// <summary>
    /// Gets or sets whether closing the dialog should be distinct from applying the draft.
    /// Existing expression editors retain their close-to-apply behavior by default.
    /// </summary>
    [Parameter] public bool RequireExplicitApply { get; set; }

    /// <summary>
    /// Gets or sets whether the editor is view-only.
    /// </summary>
    [Parameter] public bool IsReadOnly { get; set; }

    /// <summary>
    /// Gets or sets an optional validator. Return an error message to keep the dialog open.
    /// </summary>
    [Parameter] public Func<string, string?>? Validator { get; set; }

    private string EditorHeight => RequireExplicitApply ? "min(56vh, 655px)" : "655px";

    private async Task OnMonacoInitializedAsync()
    {
        _isInternalContentChange = true;
        var model = await _monacoEditor!.GetModel();
        _lastMonacoEditorContent = Value;
        await model.SetValue(Value);
        _isInternalContentChange = false;
        await Global.SetModelLanguage(JSRuntime, model, MonacoLanguage);
    }

    private StandaloneEditorConstructionOptions ConfigureMonacoEditor(StandaloneCodeEditor editor)
    {
        return new()
        {
            Language = MonacoLanguage,
            Value = Value,
            FontFamily = "Roboto Mono, monospace",
            RenderLineHighlight = "none",
            FixedOverflowWidgets = false,
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

    private async Task OnMonacoContentChangedAsync(ModelContentChangedEvent e)
    {
        if (_isInternalContentChange)
            return;

        var value = await _monacoEditor!.GetValue();
        if (value == _lastMonacoEditorContent)
            return;

        Value = value;
        _lastMonacoEditorContent = value;
        _validationError = null;
    }

    private void OnClosedClicked()
    {
        if (RequireExplicitApply)
            MudDialog.Cancel();
        else
            MudDialog.Close(DialogResult.Ok(Value));
    }

    private void Cancel() => MudDialog.Cancel();

    private void Apply()
    {
        _validationError = Validator?.Invoke(Value);
        if (_validationError == null)
            MudDialog.Close(DialogResult.Ok(Value));
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
