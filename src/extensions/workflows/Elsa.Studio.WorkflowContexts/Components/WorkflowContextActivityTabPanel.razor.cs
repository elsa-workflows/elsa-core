namespace Elsa.Studio.WorkflowContexts.Components;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;

using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowExecutionContexts.Models;
using Elsa.Api.Client.Shared.UIHints.DropDown;
using Elsa.Extensions;
using Elsa.Studio.WorkflowContexts.Contracts;
using Elsa.Studio.WorkflowContexts.Extensions;
using Elsa.Studio.WorkflowContexts.Models;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Diagnostics;
using System.Text.Json.Nodes;

public partial class WorkflowContextActivityTabPanel
{
    /// <summary>
    /// Gets or sets the workflow definition.
    /// </summary>
    [Parameter]
    public WorkflowDefinition? WorkflowDefinition { get; set; }

    /// <summary>
    /// Gets or sets the activity.
    /// </summary>
    [Parameter]
    public JsonObject? Activity { get; set; }

    /// <summary>
    /// Gets or sets the activity descriptor.
    /// </summary>
    [Parameter]
    public ActivityDescriptor? ActivityDescriptor { get; set; }

    /// <summary>
    /// Gets or sets a callback that is invoked when the activity is updated.
    /// </summary>
    [Parameter]
    public Func<JsonObject, Task>? OnActivityUpdated { get; set; }

    [Inject] private IWorkflowContextsProvider WorkflowContextsProvider { get; set; } = null!;

    [CascadingParameter] private IWorkspace? Workspace { get; set; }

    private bool IsReadOnly => Workspace?.IsReadOnly == true;

    private ICollection<SelectListItem> _items = Array.Empty<SelectListItem>();

    private Dictionary<string, ActivityWorkflowContextSettings> _contextSettings = new Dictionary<string, ActivityWorkflowContextSettings>();

    private ICollection<WorkflowContextProviderDescriptor> WorkflowContextDescriptors { get; set; } = new List<WorkflowContextProviderDescriptor>();

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        WorkflowContextDescriptors = (await WorkflowContextsProvider.ListAsync()).ToList();
    }

    protected override void OnParametersSet()
    {
        var selectedContextProviders = WorkflowDefinition!.GetWorkflowContextProviderTypes();
        _items = WorkflowContextDescriptors.Where(descriptior => selectedContextProviders.Contains(descriptior.Type))
            .Select(descriptior => new SelectListItem(descriptior.Name, descriptior.Type)).ToList();


        if (Activity != null)
        {
            _contextSettings = Activity.GetWorkflowContextSettings() ?? new Dictionary<string, ActivityWorkflowContextSettings>();
        }
    }


    private bool IsChecked(string type, string function)
    {
        try
        {
            if (_contextSettings.ContainsKey(type))
            {
                return function switch
                {
                    "Load" => _contextSettings[type].Load,
                    "Save" => _contextSettings[type].Save,
                    _ => false
                };

            }
        }
        catch
        {

        }
        return false;
    }

    private async Task OnValueChanged(string type, string function, bool? value)
    {
        if (Activity != null)
        {
            if (value.HasValue)
            {
                if (!_contextSettings.ContainsKey(type))
                {
                    _contextSettings[type] = new ActivityWorkflowContextSettings();
                }

                if (String.Equals(function, "Load", StringComparison.OrdinalIgnoreCase))
                {
                    _contextSettings[type].Load = value.Value;
                }
                if (String.Equals(function, "Save", StringComparison.OrdinalIgnoreCase))
                {
                    _contextSettings[type].Save = value.Value;
                }
            }

            Activity.SetWorkflowContextSettings(_contextSettings);


            if (OnActivityUpdated != null)
                await OnActivityUpdated(Activity);
        }
    }
}