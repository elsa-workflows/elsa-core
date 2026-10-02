using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Designer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using Refit;

namespace Elsa.Studio.Workflows.DiagramDesigners.StateMachines.Presentation;

/// <summary>
/// The authoring modes supported by the transition condition editor.
/// </summary>
public enum BooleanConditionEditorMode
{
    Always,
    Never,
    Provider,
    Custom
}

/// <summary>
/// The explicit result returned by the condition editor. The explicit Applied flag
/// distinguishes Apply-to-missing from cancelling the dialog.
/// </summary>
public sealed record BooleanConditionDialogResult(bool Applied, JsonNode? Condition);

/// <summary>
/// Provides a transactional editor for a StateMachine transition condition.
/// </summary>
public partial class BooleanConditionEditorDialog
{
    private static readonly JsonSerializerOptions PrettyJson = new() { WriteIndented = true };
    private static readonly HashSet<string> NonProviderExpressionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Literal", "Object", "Variable", "Input"
    };

    private JsonNode? _originalCondition;
    private BooleanConditionEditorMode _originalMode;
    private string? _originalProviderType;
    private bool _originalIsExplicitTrue;
    private bool _preserveExplicitTrue;
    private BooleanConditionEditorMode _mode;
    private string _customSource = "null";
    private string? _customError;
    private string? _providerType;
    private Expression? _providerExpression;
    private IReadOnlyList<ExpressionDescriptor> _providers = [];
    private string? _providerLoadError;

    [Parameter] public JsonNode? Condition { get; set; }
    [Parameter] public bool IsReadOnly { get; set; }
    [Parameter] public IReadOnlyCollection<ExpressionDescriptor>? KnownExpressionProviders { get; set; }

    [Inject] private IExpressionService ExpressionService { get; set; } = null!;
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    private bool HasProviders => _providers.Count > 0;
    private bool CustomIsValid => _customError == null;
    private bool CanApply => !IsReadOnly && (_mode != BooleanConditionEditorMode.Provider || _providerExpression != null) && (_mode != BooleanConditionEditorMode.Custom || CustomIsValid);
    private bool IsLossySwitch =>
        ((_originalMode is BooleanConditionEditorMode.Provider or BooleanConditionEditorMode.Custom) && _mode != _originalMode)
        || (_originalMode == BooleanConditionEditorMode.Provider
            && _mode == BooleanConditionEditorMode.Provider
            && !string.Equals(_originalProviderType, _providerType, StringComparison.OrdinalIgnoreCase))
        || (_originalIsExplicitTrue && _mode == BooleanConditionEditorMode.Always && !_preserveExplicitTrue);

    protected override async Task OnInitializedAsync()
    {
        await LoadProvidersAsync();
        InitializeDraft();
    }

    private async Task LoadProvidersAsync()
    {
        if (KnownExpressionProviders != null)
        {
            _providers = FilterProviders(KnownExpressionProviders);
            _providerLoadError = null;
            return;
        }

        try
        {
            var descriptors = await ExpressionService.ListDescriptorsAsync();
            _providers = FilterProviders(descriptors);
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or ApiException)
        {
            _providers = [];
            _providerLoadError = "Expression providers could not be loaded. Use Custom JSON to preserve or repair this condition.";
        }
    }

    internal static IReadOnlyList<ExpressionDescriptor> FilterProviders(IEnumerable<ExpressionDescriptor> descriptors) =>
        descriptors
            .Where(x => x.IsBrowsable && !string.IsNullOrWhiteSpace(x.Type) && !NonProviderExpressionTypes.Contains(x.Type))
            .GroupBy(x => x.Type, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToList();

    private void InitializeDraft()
    {
        _originalCondition = Condition;
        var knownProviderTypes = _providers.Select(x => x.Type).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var description = BooleanConditionAdapter.Inspect(Condition, knownProviderTypes);

        _mode = description.Kind switch
        {
            BooleanConditionKind.Missing => BooleanConditionEditorMode.Always,
            BooleanConditionKind.Literal when description.LiteralValue == false => BooleanConditionEditorMode.Never,
            BooleanConditionKind.Literal => BooleanConditionEditorMode.Always,
            BooleanConditionKind.Expression when description.ExpressionType != null => BooleanConditionEditorMode.Provider,
            _ => BooleanConditionEditorMode.Custom
        };

        _providerType = description.ExpressionType;
        _originalMode = _mode;
        _originalProviderType = description.ExpressionType;
        _originalIsExplicitTrue = description.Kind == BooleanConditionKind.Literal && description.LiteralValue == true;
        _preserveExplicitTrue = _originalIsExplicitTrue;
        _providerExpression = description.ExpressionType == null
            ? null
            : new Expression(description.ExpressionType, description.ExpressionValue ?? string.Empty);
        _customSource = Condition is JsonObject obj && StateMachineDesignerConstants.IsInvalidJsonSlotMarker(obj)
            ? obj[StateMachineDesignerConstants.InvalidJsonSlotSourceProperty]?.GetValue<string>() ?? string.Empty
            : Condition?.ToJsonString(PrettyJson) ?? "null";
        ValidateCustomSource();

        // An expression can only be edited with the provider-native editor when its
        // provider is available. Unknown expressions remain in the lossless Custom mode.
        if (_mode == BooleanConditionEditorMode.Provider && !HasProvider(_providerType))
            _mode = BooleanConditionEditorMode.Custom;
    }

    private bool HasProvider(string? type) => !string.IsNullOrWhiteSpace(type) && _providers.Any(x => string.Equals(x.Type, type, StringComparison.OrdinalIgnoreCase));

    private void SetMode(BooleanConditionEditorMode mode)
    {
        if (IsReadOnly || (mode == BooleanConditionEditorMode.Provider && !HasProviders))
            return;

        _mode = mode;
        if (mode == BooleanConditionEditorMode.Provider)
            EnsureProviderExpression();
        if (mode == BooleanConditionEditorMode.Custom)
            ValidateCustomSource();
    }

    private void EnsureProviderExpression()
    {
        if (!HasProviders)
            return;

        if (!HasProvider(_providerType))
            _providerType = _providers[0].Type;

        _providerExpression ??= new Expression(_providerType!, string.Empty);
        if (!string.Equals(_providerExpression.Type, _providerType, StringComparison.Ordinal))
            _providerExpression = new Expression(_providerType!, _providerExpression.Value?.ToString() ?? string.Empty);
    }

    private void OnProviderChanged(ChangeEventArgs args)
    {
        if (IsReadOnly)
            return;

        var type = args.Value?.ToString();
        if (!HasProvider(type))
            return;

        _providerType = type;
        _providerExpression = new Expression(type!, _providerExpression?.Value?.ToString() ?? string.Empty);
    }

    private void OnProviderInput(ChangeEventArgs args)
    {
        if (IsReadOnly || _providerExpression == null)
            return;

        _providerExpression.Value = args.Value?.ToString() ?? string.Empty;
    }

    private void OnCustomInput(ChangeEventArgs args)
    {
        if (IsReadOnly)
            return;

        _customSource = args.Value?.ToString() ?? string.Empty;
        ValidateCustomSource();
    }

    private void OnPreserveExplicitTrueChanged(ChangeEventArgs args)
    {
        if (IsReadOnly)
            return;

        _preserveExplicitTrue = args.Value is true || string.Equals(args.Value?.ToString(), "true", StringComparison.OrdinalIgnoreCase);
    }

    private void ValidateCustomSource() => _customError = BooleanConditionAdapter.TrySetAdvanced(_originalCondition, _customSource).Error;

    private void Apply()
    {
        if (!CanApply)
            return;

        var condition = _mode switch
        {
            BooleanConditionEditorMode.Always when _originalIsExplicitTrue && _preserveExplicitTrue => BooleanConditionAdapter.SetLiteral(_originalCondition, true),
            BooleanConditionEditorMode.Always => (JsonNode?)null,
            BooleanConditionEditorMode.Never => BooleanConditionAdapter.SetLiteral(_originalCondition, false),
            BooleanConditionEditorMode.Provider when _providerExpression != null => BooleanConditionAdapter.SetExpression(_originalCondition, _providerExpression.Type, _providerExpression.Value?.ToString() ?? string.Empty),
            BooleanConditionEditorMode.Custom => BooleanConditionAdapter.TrySetAdvanced(_originalCondition, _customSource).Value,
            _ => _originalCondition
        };

        MudDialog.Close(DialogResult.Ok(new BooleanConditionDialogResult(true, condition)));
    }

    private void Cancel() => MudDialog.Cancel();

    private string ModeClass(BooleanConditionEditorMode mode) => _mode == mode
        ? "state-machine-condition-editor__mode state-machine-condition-editor__mode--selected"
        : "state-machine-condition-editor__mode";

    private static string DisplayProvider(ExpressionDescriptor descriptor) => string.IsNullOrWhiteSpace(descriptor.DisplayName) ? descriptor.Type : descriptor.DisplayName;
}
