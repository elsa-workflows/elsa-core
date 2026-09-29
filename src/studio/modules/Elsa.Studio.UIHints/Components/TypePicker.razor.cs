using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;
using Elsa.Api.Client.Resources.VariableTypes.Models;
using Elsa.Studio.Workflows.Domain.Contracts;

namespace Elsa.Studio.UIHints.Components;

/// <summary>
/// Provides a component for picking a type.
/// </summary>
public partial class TypePicker
{
    private ICollection<VariableTypeDescriptor> _variableTypes = new List<VariableTypeDescriptor>();
    private ICollection<IGrouping<string, VariableTypeDescriptor>> _groupedVariableTypes = new List<IGrouping<string, VariableTypeDescriptor>>();

    /// <summary>
    /// Gets or sets the editor context.
    /// </summary>
    [Parameter] public DisplayInputEditorContext EditorContext { get; set; } = null!;

    [Inject] private IVariableTypeService VariableTypeService { get; set; } = null!;

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        _variableTypes = (await VariableTypeService.GetVariableTypesAsync()).ToList();
        _groupedVariableTypes = _variableTypes.GroupBy(x => x.Category).ToList();
    }

    private VariableTypeDescriptor? GetSelectedValue()
    {
        var value = EditorContext.GetLiteralValueOrDefault();
        return _variableTypes.FirstOrDefault(x => x.TypeName == value);
    }

    private async Task OnValueChanged(VariableTypeDescriptor? value)
    {
        await EditorContext.UpdateValueOrLiteralExpressionAsync(value?.TypeName ?? "");
    }
}