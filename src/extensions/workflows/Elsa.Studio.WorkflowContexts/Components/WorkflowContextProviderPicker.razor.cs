using Elsa.Api.Client.Shared.UIHints.DropDown;
using Elsa.Api.Client.Resources.WorkflowExecutionContexts.Models;
using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;
using Elsa.Extensions;
using Elsa.Studio.WorkflowContexts.Contracts;
using Elsa.Api.Client.Resources.Scripting.Models;

namespace Elsa.Studio.WorkflowContexts.Components;

/// <summary>
/// A component that renders the workflow context drop down.
/// </summary>
public partial class WorkflowContextProviderPicker
{
    private ICollection<SelectListItem> _items = Array.Empty<SelectListItem>();

    /// <summary>
    /// Gets or sets the editor context.
    /// </summary>
    [Parameter] public DisplayInputEditorContext EditorContext { get; set; } = null!;

    [Inject] private IWorkflowContextsProvider WorkflowContextsProvider { get; set; } = null!;

    private ICollection<WorkflowContextProviderDescriptor> WorkflowContextDescriptors { get; set; } = new List<WorkflowContextProviderDescriptor>();

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        WorkflowContextDescriptors = (await WorkflowContextsProvider.ListAsync()).ToList();
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        var selectedContextProviders = EditorContext.WorkflowDefinition.GetWorkflowContextProviderTypes();
        _items = WorkflowContextDescriptors.Where(descriptior => selectedContextProviders.Contains(descriptior.Type))
            .Select(descriptior => new SelectListItem(descriptior.Name, descriptior.Type)).ToList();
    }

    private SelectListItem? GetSelectedValue()
    {
        var value = EditorContext.GetLiteralValueOrDefault();
        return _items.FirstOrDefault(x => x.Value == value);
    }
    
    private async Task OnValueChanged(SelectListItem? value)
    {
        var expression = Expression.CreateLiteral(value?.Value ?? "");
        await EditorContext.UpdateExpressionAsync(expression);
    }
}