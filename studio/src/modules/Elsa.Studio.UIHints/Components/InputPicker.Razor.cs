using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Api.Client.Shared.UIHints.DropDown;
using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace Elsa.Studio.UIHints.Components;

/// <summary>
/// Provides a component for picking a variable.
/// </summary>
public partial class InputPicker
{
    private ICollection<SelectListItem> _items = Array.Empty<SelectListItem>();

    /// <summary>
    /// Gets or sets the editor context.
    /// </summary>
    [Parameter] public DisplayInputEditorContext EditorContext { get; set; } = default!;

    private ICollection<InputDefinition> Inputs => EditorContext.WorkflowDefinition.Inputs;

    private JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _items = Inputs.Select(x =>
        {
            var text = $"{x.Name} - {x.DisplayName ?? x.GetTypeDisplayName()}";

            return new SelectListItem(text, x.Name);
        }).OrderBy(x => x.Text).ToList();
    }

    private SelectListItem? GetSelectedValue()
    {
        InputDefinition? value;
        if (EditorContext.InputDescriptor.IsWrapped)
        {
            var expressionValue = EditorContext.GetExpressionValueOrDefault();

            if (string.IsNullOrEmpty(expressionValue))
            {
                value = null;
            }
            else
            {
                try
                {
                    value = JsonSerializer.Deserialize<InputDefinition>(expressionValue, JsonSerializerOptions);
                }
                catch (Exception)
                {
                    value = null;
                }
            }
        }
        else
        {
            value = EditorContext.GetValueOrDefault<InputDefinition>();
        }

        return _items.FirstOrDefault(x => x.Value == value?.Name);
    }

    private async Task OnValueChanged(SelectListItem? value)
    {
        var input = Inputs.FirstOrDefault(x => x.Name == value?.Value);
        if (input == null) return;

        if (EditorContext.InputDescriptor.IsWrapped)
        {
            await EditorContext.UpdateExpressionAsync(new("Input", JsonSerializer.Serialize(input, JsonSerializerOptions)));
        }
        else
        {
            await EditorContext.UpdateValueAsync(input);
        }
    }
}