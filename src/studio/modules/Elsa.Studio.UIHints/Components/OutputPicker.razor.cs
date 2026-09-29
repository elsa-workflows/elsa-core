using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Shared.UIHints.DropDown;
using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHints.Components;

/// <summary>
/// A component that renders a dropdown for outputs.
/// </summary>
public partial class OutputPicker
{
    private ICollection<SelectListItem> _items = Array.Empty<SelectListItem>();

    /// <summary>
    /// The editor context.
    /// </summary>
    [Parameter] public DisplayInputEditorContext EditorContext { get; set; } = null!;

    private ICollection<OutputDefinition> Outputs => EditorContext.WorkflowDefinition.Outputs;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        var items = Outputs.Select(x => new SelectListItem(x.DisplayName, x.Name)).OrderBy(x => x.Text).ToList();
        items.Insert(0, new("(None)", ""));
        _items = items;
    }

    private SelectListItem? GetSelectedValue()
    {
        var outputName = EditorContext.GetExpressionValueOrDefault();
        return _items.FirstOrDefault(x => x.Value == outputName);
    }

    private async Task OnValueChanged(SelectListItem? value)
    {
        var outputName = value?.Value ?? "";
        await EditorContext.UpdateValueOrLiteralExpressionAsync(outputName);
    }
}