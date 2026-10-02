using Elsa.Api.Client.Shared.UIHints.RadioList;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Extensions;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHints.Components;

/// <summary>
/// A component that renders a radio list.
/// </summary>
public partial class RadioList
{
    private ICollection<RadioListItem> _radioListItems = [];
    private string _selectedValue = string.Empty;

    /// <summary>
    /// Represents the selected radio item value in the radio list component.
    /// Updates the selection and notifies the associated editor context when changed.
    /// </summary>
    public string SelectedValue
    {
        get => _selectedValue;
        set
        {
            _selectedValue = value;
            _ = OnValueChanged(value);
        }
    }

    /// <summary>
    /// The editor context.
    /// </summary>
    [Parameter] public DisplayInputEditorContext EditorContext { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        var radioList = EditorContext.InputDescriptor.GetRadioList();
        _radioListItems = radioList.Items.ToList();

        var value = EditorContext.GetLiteralValueOrDefault();
        _selectedValue = _radioListItems.FirstOrDefault(x => x.Value == value)?.Value ?? "";
    }

    private async Task OnValueChanged(string? value)
    {
        await EditorContext.UpdateValueOrLiteralExpressionAsync(value ?? "");
    }
}