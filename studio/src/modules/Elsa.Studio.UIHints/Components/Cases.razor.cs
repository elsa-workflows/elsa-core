using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Helpers;
using Elsa.Studio.Workflows.Domain.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.UIHints.Components;

/// <summary>
/// Renders an editor for a collection of <see cref="SwitchCase"/> objects.
/// </summary>
public partial class Cases
{
    private readonly string[] _uiSyntaxes = { "Literal", "Object" };

    private SwitchCaseRecord? _caseBeingEdited;
    private SwitchCaseRecord? _caseBeingAdded;
    private MudTable<SwitchCaseRecord> _table = default!;

    /// <summary>
    /// The context for the editor.
    /// </summary>
    [Parameter]
    /// <summary>
    /// Gets or sets the editor context.
    /// </summary>
    public DisplayInputEditorContext EditorContext { get; set; } = default!;

    [CascadingParameter] private ExpressionDescriptorProvider ExpressionDescriptorProvider { get; set; } = default!;

    private ICollection<SwitchCaseRecord> Items { get; set; } = new List<SwitchCaseRecord>();
    private bool DisableAddButton => _caseBeingEdited != null || _caseBeingAdded != null;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        Items = GetItems();
    }

    private ICollection<SwitchCaseRecord> GetItems()
    {
        var input = EditorContext.GetObjectValueOrDefault();
        var cases = ParseJson(input);
        var caseRecords = cases.Select(Map).ToList();
        return caseRecords;
    }

    private IEnumerable<SwitchCase> ParseJson(string? json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonParser.ParseJson(json, () => new List<SwitchCase>(), options);
    }

    private IEnumerable<ExpressionDescriptor> GetSupportedExpressions()
    {
        return ExpressionDescriptorProvider.ListDescriptors().Where(x => !_uiSyntaxes.Contains(x.Type) && x.IsBrowsable).ToList();
    }

    private string GetDefaultExpressionType()
    {
        var defaultExpressionType = GetSupportedExpressions().FirstOrDefault()?.Type ?? "Literal";
        return defaultExpressionType;
    }

    private SwitchCaseRecord Map(SwitchCase @case)
    {
        var defaultExpressionType = GetDefaultExpressionType();

        return new()
        {
            Label = @case.Label,
            Condition = @case.Condition.ToString(),
            ExpressionType = string.IsNullOrWhiteSpace(@case.Condition.Type) ? defaultExpressionType : @case.Condition.Type,
            Activity = @case.Activity
        };
    }

    private SwitchCase Map(SwitchCaseRecord switchCase)
    {
        var expression = new Expression(switchCase.ExpressionType, switchCase.Condition);

        return new()
        {
            Label = switchCase.Label,
            Condition = expression,
            Activity = switchCase.Activity
        };
    }

    private Task SaveChangesAsync()
    {
        var cases = Items.Select(Map).ToList();

        return EditorContext.UpdateValueAsync(cases);
    }

    private async void OnRowEditCommitted(object data)
    {
        _caseBeingAdded = null;
        _caseBeingEdited = null;
        await SaveChangesAsync();
        StateHasChanged();
    }

    private void OnRowEditPreview(object obj)
    {
        var @case = (SwitchCaseRecord)obj;
        _caseBeingEdited = new()
        {
            Label = @case.Label,
            Condition = @case.Condition,
            ExpressionType = @case.ExpressionType
        };

        StateHasChanged();
    }

    private async void OnRowEditCancel(object obj)
    {
        if (_caseBeingAdded != null)
        {
            Items.Remove(_caseBeingAdded);
            await SaveChangesAsync();
            _caseBeingAdded = null;
            StateHasChanged();
            return;
        }

        var @case = (SwitchCaseRecord)obj;
        @case.Condition = _caseBeingEdited?.Condition ?? "";
        @case.Label = _caseBeingEdited?.Label ?? "";
        @case.ExpressionType = _caseBeingEdited?.ExpressionType ?? "";
        _caseBeingEdited = null;
        StateHasChanged();
    }

    private async Task OnDeleteClicked(SwitchCaseRecord switchCase)
    {
        Items.Remove(switchCase);
        await SaveChangesAsync();
    }

    private void OnAddClicked()
    {
        var @case = new SwitchCaseRecord
        {
            Label = $"Case {Items.Count + 1}",
            Condition = "",
            ExpressionType = GetDefaultExpressionType()
        };

        Items.Add(@case);
        _caseBeingAdded = @case;

        // Need to do it this way, otherwise MudTable doesn't show the item in edit mode.
        _ = Task.Delay(1).ContinueWith(_ =>
        {
            InvokeAsync(() =>
            {
                _table.SetEditingItem(@case);
                StateHasChanged();
            });
        });
    }

    private string GetExpressionTypeDisplayName(string expressionType)
    {
        var expressionDescriptor = ExpressionDescriptorProvider.GetByType(expressionType) ?? throw new($"Could not find expression descriptor for expression type '{expressionType}'.");
        return expressionDescriptor.DisplayName;
    }
}

/// <summary>
/// Represents a single case in a <see cref="Switch"/> activity.
/// </summary>
public class SwitchCaseRecord
{
    /// <summary>
    /// The label of the case.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// The condition of the case.
    /// </summary>
    public string Condition { get; set; } = string.Empty;

    [Obsolete("Use ExpressionType instead.")]
    /// <summary>
    /// Provides the syntax.
    /// </summary>
    public string Syntax
    {
        get => ExpressionType;
        set => ExpressionType = value;
    }

    /// <summary>
    /// The expression type of the case.
    /// </summary>
    public string ExpressionType { get; set; } = "Literal";

    /// <summary>
    /// When used in a <see cref="Switch"/> activity, specifies the activity to schedule when the condition evaluates to true.
    /// </summary>
    public JsonObject? Activity { get; set; }
}