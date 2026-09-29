using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Shared.UIHints.DropDown;
using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace Elsa.Studio.UIHints.Components;

/// <summary>
/// Provides a component for picking a variable.
/// </summary>
public partial class VariablePicker
{
    private ICollection<SelectListItem> _items = Array.Empty<SelectListItem>();

    /// <summary>
    /// Gets or sets the editor context.
    /// </summary>
    [Parameter] public DisplayInputEditorContext EditorContext { get; set; } = default!;

    private ICollection<Variable> Variables => EditorContext.WorkflowDefinition.Variables;

    private JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _items = Variables.Select(x => new SelectListItem(x.Name, x.Id)).OrderBy(x => x.Text).ToList();
    }

    private SelectListItem? GetSelectedValue()
    {
        Variable? value;
        if (EditorContext.InputDescriptor.IsWrapped)
        {
            var expressionValue = EditorContext.GetExpressionValueOrDefault();
            if (string.IsNullOrEmpty(expressionValue))
                value = null;
            else
            {
                try
                {
                    value = JsonSerializer.Deserialize<Variable>(expressionValue, JsonSerializerOptions);
                }
                catch (Exception)
                {
                    value = null;   
                }
            }
        }
        else
            value = EditorContext.GetValueOrDefault<Variable>();
        
        return _items.FirstOrDefault(x => x.Value == value?.Id);
    }
    
    private async Task OnValueChanged(SelectListItem? value)
    {
        var variableId = value?.Value;
        var variable = Variables.FirstOrDefault(x => x.Id == variableId);

        if(EditorContext.InputDescriptor.IsWrapped)
           await EditorContext.UpdateExpressionAsync(new("Variable", JsonSerializer.Serialize(variable, JsonSerializerOptions)));
        else
            await EditorContext.UpdateValueAsync(variable);
    }
}