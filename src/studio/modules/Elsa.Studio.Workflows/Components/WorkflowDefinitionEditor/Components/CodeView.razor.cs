using BlazorMonaco.Editor;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Text.Json;
using ThrottleDebounce;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components;

/// <summary>
/// Provides a Monaco-based code editor for viewing and editing workflow definitions as JSON.
/// </summary>
public partial class CodeView : IDisposable
{
    private readonly string _monacoEditorId = $"monaco-editor-{Guid.NewGuid():N}";
    private StandaloneCodeEditor? _monacoEditor;
    private bool _isInternalContentChange;
    private string? _lastMonacoEditorContent;
    private string _applyErrorMessage = string.Empty;
    private RateLimitedFunc<Task>? _throttledValueChanged;

    [Inject] private ILocalizer Localizer { get; set; } = null!;
    [Inject] protected IUserMessageService UserMessageService { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the component is in read-only mode.
    /// </summary>
    [Parameter] public bool IsReadOnly { get; set; }

    /// <summary>
    /// Gets or sets the serialized representation of the workflow definition.
    /// </summary>
    [Parameter] public string WorkflowDefinitionSerialized { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the callback function to apply a workflow definition asynchronously.
    /// </summary>
    [Parameter] public Func<WorkflowDefinition, Task>? ApplyWorkflowDefinition { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether changes are applied automatically without requiring explicit user
    /// action.
    /// </summary>
    [Parameter] public bool AutoApply { get; set; }

    /// <summary>
    /// Gets or sets the callback that is invoked when the auto-apply setting changes.
    /// </summary>
    [Parameter] public EventCallback<bool> AutoApplyChanged { get; set; }

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        _throttledValueChanged = Debouncer.Debounce(UpdateEditorFromCodeViewAsync, TimeSpan.FromMilliseconds(500));
        _lastMonacoEditorContent = WorkflowDefinitionSerialized;
    }

    /// <summary>
    /// Updates the code view in the editor with the specified JSON content asynchronously.
    /// </summary>
    /// <param name="json">The JSON string representing the new content to display in the code editor.</param>
    public async Task UpdateCodeViewFromEditorAsync(string json)
    {
        _isInternalContentChange = true;
        if (_monacoEditor is null)
            return;

        var model = await _monacoEditor.GetModel();
        if (json == _lastMonacoEditorContent)
            return;

        _lastMonacoEditorContent = json;
        await model.SetValue(json);
    }

    private StandaloneEditorConstructionOptions ConfigureMonacoEditor(StandaloneCodeEditor editor)
    {
        _applyErrorMessage = string.Empty;
        return new()
        {
            Language = "json",
            Value = WorkflowDefinitionSerialized,
            FontFamily = "Roboto Mono, monospace",
            RenderLineHighlight = "none",
            FixedOverflowWidgets = true,
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
        if (_isInternalContentChange || !AutoApply || IsReadOnly)
            return;

        if (_throttledValueChanged is not null)
            await _throttledValueChanged.InvokeAsync();
    }

    private async Task OnApplyClicked()
    {
        var success = await UpdateEditorFromCodeViewAsync();
        if (success)
            UserMessageService.ShowSnackbarTextMessage(Localizer["Changes applied"], Severity.Success);
    }

    private async Task<bool> UpdateEditorFromCodeViewAsync()
    {
        var value = await _monacoEditor!.GetValue();
        if (string.Equals(value, _lastMonacoEditorContent))
        {
            UserMessageService.ShowSnackbarTextMessage(Localizer["No changes to apply"], Severity.Info);
            return false;
        }

        _lastMonacoEditorContent = value;
        WorkflowDefinition? deserialized = null;
        try
        {
            try
            {
                deserialized = JsonSerializer.Deserialize<WorkflowDefinition?>(value);
            }
            catch (JsonException ex)
            {
                _applyErrorMessage = ex.Message;
                return false;
            }

            if (deserialized == null)
            {
                _applyErrorMessage = Localizer["Failed to deserialize workflow definition."];
                return false;
            }

            _applyErrorMessage = string.Empty;
        }
        finally
        {
            await InvokeAsync(StateHasChanged);
        }

        if (ApplyWorkflowDefinition is null)
            return true;

        try
        {
            await ApplyWorkflowDefinition(deserialized);
            return true;
        }
        catch (Exception ex)
        {
            _applyErrorMessage = ex.Message;
            await InvokeAsync(StateHasChanged);
            return false;
        }
    }

    private async Task ReloadMonacoClick()
    {
        _isInternalContentChange = true;
        var model = await _monacoEditor!.GetModel();
        await model.SetValue(WorkflowDefinitionSerialized);
        _lastMonacoEditorContent = WorkflowDefinitionSerialized;
    }

    private async Task OnAutoApplyChanged(bool value)
    {
        AutoApply = value;
        if (AutoApplyChanged.HasDelegate)
            await AutoApplyChanged.InvokeAsync(AutoApply);

        if (AutoApply)
            await UpdateEditorFromCodeViewAsync();
    }

    void IDisposable.Dispose() => _throttledValueChanged?.Dispose();
}
