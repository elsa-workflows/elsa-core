using BlazorMonaco.Editor;
using Elsa.Api.Client.Resources.Scripting.Extensions;
using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using ThrottleDebounce;

namespace Elsa.Studio.Components;

/// <summary>
/// A component that renders an input for an expression.
/// </summary>
public partial class ExpressionEditor : IDisposable
{
    private readonly string[] _expressionTypeBlacklist = ["Literal", "Object", "Variable", "Input"];
    private readonly string _monacoEditorId = $"monaco-editor-{Guid.NewGuid():N}";
    private string? _selectedExpressionType;
    private StandaloneCodeEditor? _monacoEditor;
    private bool _isInternalContentChange;
    private bool _isInitialized;
    private string? _lastMonacoEditorContent;
    private readonly RateLimitedFunc<Expression, Task> _throttledValueChanged;
    private ICollection<ExpressionDescriptor> _expressionDescriptors = new List<ExpressionDescriptor>();
    private TaskCompletionSource<bool>? _initializationTcs;

    /// <inheritdoc />
    public ExpressionEditor()
    {
        _throttledValueChanged = Debouncer.Debounce<Expression, Task>(InvokeValueChangedCallbackAsync, TimeSpan.FromMilliseconds(500));
    }

    /// <summary>
    /// The content to render inside the editor.
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; } = null!;

    /// <summary>
    /// Gets or sets the expression to be edited.
    /// </summary>
    [Parameter] public Expression? Expression { get; set; } = new(string.Empty, string.Empty);

    /// <summary>
    /// Gets or sets the default option to be used when no specific expression type is selected.
    /// </summary>
    [Parameter] public string DefaultOption { get; set; } = "Default";

    /// <summary>
    /// The display name associated with the expression being edited.
    /// </summary>
    [Parameter] public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the expression editor, providing contextual information
    /// or guidance for the expression being entered by the user.
    /// </summary>
    [Parameter] public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the expression editor is in read-only mode.
    /// Prevents modifications to the expression when set to true.
    /// </summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>
    /// A dictionary of custom properties that can be used to provide additional metadata
    /// or configuration for the expression editor.
    /// </summary>
    [Parameter] public IDictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// A callback function that is invoked when the expression is changed.
    /// </summary>
    [Parameter] public Func<Expression?, Task>? ExpressionChanged { get; set; }

    [Inject] private TypeDefinitionService TypeDefinitionService { get; set; } = null!;
    [Inject] private IExpressionService ExpressionService { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] private IEnumerable<IMonacoHandler> MonacoHandlers { get; set; } = null!;
    [Inject] private IUIHintService UIHintService { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    private IEnumerable<ExpressionDescriptor> BrowsableExpressionDescriptors => _expressionDescriptors.Where(x => x.IsBrowsable);
    private ExpressionDescriptor? SelectedExpressionDescriptor => _expressionDescriptors.FirstOrDefault(x => x.Type == _selectedExpressionType);
    private string SelectedExpressionTypeDisplayName => SelectedExpressionDescriptor?.DisplayName ?? DefaultOption;
    private string MonacoLanguage => SelectedExpressionDescriptor?.GetMonacoLanguage() ?? string.Empty;
    private bool DisplayCodeEditor => _selectedExpressionType != null && _selectedExpressionType != DefaultOption;
    private bool DisplayModePicker => !DisplayCodeEditor;
    private string? ButtonIcon => DisplayModePicker ? Icons.Material.Filled.MoreVert : null;
    private string? ButtonLabel => DisplayModePicker ? null : SelectedExpressionTypeDisplayName;
    private Variant ButtonVariant => DisplayModePicker ? default : Variant.Filled;
    private Color ButtonColor => DisplayModePicker ? default : Color.Primary;
    private string? ButtonEndIcon => DisplayModePicker ? null : Icons.Material.Filled.KeyboardArrowDown;
    private Color ButtonEndColor => DisplayModePicker ? default : Color.Secondary;

    private bool MonacoSyntaxExist => !string.IsNullOrEmpty(MonacoLanguage);

    /// <summary>
    /// Updates the internal state of the editor with the provided expression.
    /// </summary>
    /// <param name="expression">The expression to update the editor with. Can be null.</param>
    public async Task UpdateAsync(Expression? expression)
    {
        if (!_isInitialized || _monacoEditor == null)
        {
            _initializationTcs ??= new();
            await _initializationTcs.Task;
        }

        await UpdateMonacoEditorAsync(expression);
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var expressionDescriptors = (await ExpressionService.ListDescriptorsAsync()).ExceptBy(_expressionTypeBlacklist, x => x.Type).ToList();
        _selectedExpressionType = expressionDescriptors.First().Type;
        _expressionDescriptors = expressionDescriptors.ToList();
    }

    private async Task UpdateMonacoLanguageAsync()
    {
        if (_monacoEditor == null)
            return;

        if (SelectedExpressionDescriptor?.Type == null)
            return;

        if (string.IsNullOrWhiteSpace(MonacoLanguage))
            return;

        var model = await _monacoEditor!.GetModel();
        await Global.SetModelLanguage(JSRuntime, model, MonacoLanguage);
        await RunMonacoHandlersAsync(_monacoEditor);
    }

    /// <inheritdoc />
    protected override Task OnParametersSetAsync()
    {
        _selectedExpressionType = Expression?.Type;
        return base.OnParametersSetAsync();
    }

    private StandaloneEditorConstructionOptions ConfigureMonacoEditor(StandaloneCodeEditor editor)
    {
        return new()
        {
            Language = MonacoLanguage,
            Value = Expression?.Value?.ToString() ?? "",
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
            ReadOnly = ReadOnly,
            DomReadOnly = ReadOnly
        };
    }

    private async Task OnSyntaxSelectedAsync(string? syntax)
    {
        _selectedExpressionType = syntax;

        var value = Expression?.Value?.ToString() ?? "";
        var expressionType = syntax ?? DefaultOption;
        var expression = new Expression(expressionType, value);
        await InvokeValueChangedCallbackAsync(expression);
        await UpdateMonacoLanguageAsync();
        StateHasChanged();
    }

    private async Task OnMonacoContentChangedAsync(ModelContentChangedEvent e)
    {
        if (_isInternalContentChange)
            return;

        var value = await _monacoEditor!.GetValue();

        // This event gets fired even when the content hasn't changed, for example, when the containing pane is resized.
        // This happens from within the monaco editor itself (or the Blazor wrapper, not sure).
        if (value == _lastMonacoEditorContent)
            return;

        var expression = new Expression(_selectedExpressionType!, value);
        _lastMonacoEditorContent = value;
        await ThrottleValueChangedCallbackAsync(expression);
    }

    private async Task ThrottleValueChangedCallbackAsync(Expression expression) => await _throttledValueChanged.InvokeAsync(expression);

    private async Task InvokeValueChangedCallbackAsync(Expression? expression)
    {
        await InvokeAsync(async () =>
        {
            if (ExpressionChanged != null)
                await ExpressionChanged.Invoke(expression);
        });
    }

    private async Task OnMonacoInitializedAsync()
    {
        _isInitialized = true;

        if (_initializationTcs != null)
        {
            _initializationTcs.SetResult(true);
            _initializationTcs = null;
            return;
        }

        await UpdateMonacoEditorAsync(Expression);
    }

    private async Task UpdateMonacoEditorAsync(Expression? expression)
    {
        _isInternalContentChange = true;
        var model = await _monacoEditor!.GetModel();
        var expressionText = expression?.Value?.ToString() ?? string.Empty;

        if (expression?.Type != null)
            _selectedExpressionType = expression.Type;

        _lastMonacoEditorContent = expressionText;
        await model.SetValue(expressionText);
        _isInternalContentChange = false;
        await Global.SetModelLanguage(JSRuntime, model, MonacoLanguage);
        await RunMonacoHandlersAsync(_monacoEditor);
    }

    private async Task RunMonacoHandlersAsync(StandaloneCodeEditor editor)
    {
        if (SelectedExpressionDescriptor == null)
            return;

        var context = new MonacoContext(editor, SelectedExpressionDescriptor, CustomProperties);

        foreach (var handler in MonacoHandlers)
            await handler.InitializeAsync(context);
    }

    private async Task ShowScriptEditor()
    {
        var currentValue = _lastMonacoEditorContent ?? Expression?.Value?.ToString() ?? string.Empty;
        var languageLabel = SelectedExpressionDescriptor?.Type ?? ButtonLabel;

        var param = new DialogParameters<CodeEditorDialog>
        {
            { x => x.Label, DisplayName },
            { x => x.HelperText, Description },
            { x => x.Value, currentValue },
            { x => x.LanguageLabel, languageLabel },
            { x => x.MonacoLanguage, MonacoLanguage },
        };

        var options = new DialogOptions { CloseOnEscapeKey = true, Position = DialogPosition.Center, FullWidth = true, MaxWidth = MaxWidth.Large };
        var dialogRef = await DialogService.ShowAsync<CodeEditorDialog>(DisplayName, param, options);
        var dialogResult = await dialogRef.Result;

        if (dialogResult?.Data is string newValue && newValue != currentValue)
        {
            _lastMonacoEditorContent = newValue;

            // Map dialog result back to the Expression model and notify the consumer.
            var expressionType = _selectedExpressionType ?? DefaultOption;
            var updatedExpression = new Expression(expressionType, newValue);

            Expression = updatedExpression;
            if (ExpressionChanged != null)
                await ExpressionChanged.Invoke(Expression);

            await UpdateMonacoEditorAsync(Expression);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _throttledValueChanged.Dispose();
    }
}