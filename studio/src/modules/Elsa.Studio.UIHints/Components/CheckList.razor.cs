using Elsa.Api.Client.Shared.UIHints.CheckList;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Extensions;
using Elsa.Studio.UIHints.Helpers;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHints.Components;

/// <summary>
/// A component that renders a check list.
/// </summary>
public partial class CheckList
{
    private ICollection<CheckListItem> _checkListItems = [];

    /// <summary>
    /// The editor context.
    /// </summary>
    [Parameter] public DisplayInputEditorContext EditorContext { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        var checkList = EditorContext.InputDescriptor.GetCheckList();
        var selectedValues = GetSelectedValues(checkList.IsFlagsEnum);

        _checkListItems = checkList.Items
            .Select(i => new CheckListItem { Value = i.Value, Text = i.Text, IsChecked = selectedValues.Contains(i.Value) })
            .ToList();
    }

    private ICollection<string> GetSelectedValues(bool isFlagsEnum)
    {
        var inputDescriptor = EditorContext.InputDescriptor;
        var input = EditorContext.GetObjectValueOrDefault();
        var defaultValue = inputDescriptor.DefaultValue;
        var json = !string.IsNullOrWhiteSpace(input) ? input : defaultValue as string;

        if (isFlagsEnum)
        {
            return !string.IsNullOrWhiteSpace(json)
                ? int.TryParse(json, out var n) ? [n.ToString()] : Array.Empty<string>()
                : [defaultValue?.ToString() ?? string.Empty];
        }

        var selectListItems = ParseJson(json);

        if (!selectListItems.Any())
            selectListItems = ParseJson(defaultValue as string);

        return selectListItems;
    }

    private ICollection<string> ParseJson(string? json)
    {
        return JsonParser.ParseJson<ICollection<string>>(json, () => new List<string>());
    }

    private async Task OnCheckedChanged(CheckListItem item, bool? state)
    {
        // Toggle state.
        item.IsChecked = state == true;

        // Get selected values.
        var selectedValues = _checkListItems.Where(x => x.IsChecked).Select(x => x.Value).ToList();

        // Update state.
        await EditorContext.UpdateValueOrObjectExpressionAsync(selectedValues);
    }
}