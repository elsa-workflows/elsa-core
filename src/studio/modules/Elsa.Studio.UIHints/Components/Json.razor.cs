using BlazorMonaco.Editor;
using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Api.Client.Shared.UIHints.CodeEditor;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Extensions;
using Microsoft.AspNetCore.Components;
using ThrottleDebounce;

namespace Elsa.Studio.UIHints.Components;

/// <summary>
/// A component that renders a code editor.
/// </summary>
public partial class Json : IDisposable
{
    private readonly string _monacoEditorId = $"monaco-editor-{Guid.NewGuid():N}";
    private StandaloneCodeEditor? _monacoEditor;
    private string? _lastMonacoEditorContent;
    private readonly RateLimitedFunc<Task> _throttledValueChanged;
    private CodeEditorOptions _codeEditorOptions = new();

    /// <inheritdoc />
    public Json()
    {
        _throttledValueChanged = Debouncer.Debounce(InvokeValueChangedCallback, TimeSpan.FromMilliseconds(500));
    }

    /// <summary>
    /// The context for the editor.
    /// </summary>
    [Parameter] public DisplayInputEditorContext EditorContext { get; set; } = null!;

    private string InputValue => EditorContext.GetLiteralValueOrDefault();

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _codeEditorOptions = EditorContext.InputDescriptor.GetCodeEditorOptions();
    }

    private StandaloneEditorConstructionOptions ConfigureMonacoEditor(StandaloneCodeEditor editor)
    {
        var language = _codeEditorOptions.Language ?? "javascript";
        
        return new()
        {
            Language = language,
            Value = InputValue,
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
            ReadOnly = EditorContext.IsReadOnly,
            DomReadOnly = EditorContext.IsReadOnly
        };
    }

    private async Task OnMonacoContentChanged(ModelContentChangedEvent e)
    {
        await _throttledValueChanged.InvokeAsync();
    }

    private async Task InvokeValueChangedCallback()
    {
        var value = await _monacoEditor!.GetValue();

        // This event gets fired even when the content hasn't changed, but for example when the containing pane is resized.
        // This happens from within the monaco editor itself (or the Blazor wrapper, not sure).
        if (value == _lastMonacoEditorContent)
            return;

        _lastMonacoEditorContent = value;
        var expression = Expression.CreateLiteral(value);

        await InvokeAsync(async () => await EditorContext.UpdateExpressionAsync(expression));
    }

    void IDisposable.Dispose() => _throttledValueChanged.Dispose();
}