using Elsa.Api.Client.Shared.UIHints.DropDown;
using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHints.Components;

/// <summary>
/// A component that renders a dropdown for outcomes.
/// </summary>
public partial class OutcomePicker
{
    private ICollection<SelectListItem> _items = Array.Empty<SelectListItem>();

    /// <summary>
    /// The editor context.
    /// </summary>
    [Parameter] public DisplayInputEditorContext EditorContext { get; set; } = null!;

    private ICollection<string> Outcomes => EditorContext.WorkflowDefinition.Outcomes;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _items = Outcomes.Select(x => new SelectListItem(x, x)).OrderBy(x => x.Text).ToList();
    }

    private SelectListItem? GetSelectedValue()
    {
        var value = EditorContext.GetLiteralValueOrDefault();
        return _items.FirstOrDefault(x => x.Value == value);
    }

    private async Task OnValueChanged(SelectListItem? value)
    {
        var outcome = value?.Value ?? "";
        await EditorContext.UpdateValueOrLiteralExpressionAsync(outcome);
    }
}