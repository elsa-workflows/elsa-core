using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptorOptions.Contracts;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.UI.Contracts;
using Humanizer;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs;

/// <summary>
/// A tab for editing the inputs of an activity.
/// </summary>
public partial class InputsTab
{
    /// <summary>
    /// Gets or sets the workflow definition.
    /// </summary>
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }

    /// <summary>
    /// Gets or sets the activity to edit.
    /// </summary>
    [Parameter] public JsonObject? Activity { get; set; }

    /// <summary>
    /// Gets or sets the activity descriptor.
    /// </summary>
    [Parameter] public ActivityDescriptor? ActivityDescriptor { get; set; }

    /// <summary>
    /// An event that is invoked when the activity is updated.
    /// </summary>
    [Parameter] public Func<JsonObject, Task>? OnActivityUpdated { get; set; }

    [CascadingParameter] private IWorkspace? Workspace { get; set; }
    [CascadingParameter] private ExpressionDescriptorProvider ExpressionDescriptorProvider { get; set; } = null!;
    [Inject] private IUIHintService UIHintService { get; set; } = null!;

    [Inject] private IBackendApiClientProvider BackendApiClientProvider { get; set; } = null!;
    private ICollection<InputDescriptor> InputDescriptors { get; set; } = new List<InputDescriptor>();
    private ICollection<OutputDescriptor> OutputDescriptors { get; set; } = new List<OutputDescriptor>();
    private ICollection<ActivityInputDisplayModel> InputDisplayModels { get; set; } = new List<ActivityInputDisplayModel>();
    private string? _lastActivityId;
    private string? _lastActivityDescriptorTypeName;

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (Activity == null || ActivityDescriptor == null)
            return;

        // Detect if the activity or descriptor actually changed (not just a value update)
        var currentActivityId = Activity.GetProperty("id")?.ToString();
        var currentDescriptorTypeName = ActivityDescriptor.TypeName;
        var activityOrDescriptorChanged = currentActivityId != _lastActivityId || currentDescriptorTypeName != _lastActivityDescriptorTypeName;

        InputDescriptors = ActivityDescriptor.Inputs.ToList();
        OutputDescriptors = ActivityDescriptor.Outputs.ToList();

        // Only rebuild display models if the activity/descriptor actually changed
        if (activityOrDescriptorChanged || InputDisplayModels.Count == 0)
        {
            // Yield to allow the Blazor renderer to complete the disposal cycle.
            // This ensures BlazorMonaco's internal editor registry is updated before we create new editors.
            await Task.Yield();
            
            InputDisplayModels = (await BuildInputEditorModels()).ToList();
            _lastActivityId = currentActivityId;
            _lastActivityDescriptorTypeName = currentDescriptorTypeName;
        }
    }

    private Task<IEnumerable<ActivityInputDisplayModel>> BuildInputEditorModels()
    {
        return BuildInputEditorModels(Activity!, ActivityDescriptor!, InputDescriptors);
    }

    private async Task<IEnumerable<ActivityInputDisplayModel>> BuildInputEditorModels(JsonObject activity, ActivityDescriptor activityDescriptor, ICollection<InputDescriptor> inputDescriptors)
    {
        var models = new List<ActivityInputDisplayModel>();
        var browsableInputDescriptors = inputDescriptors.Where(x => x.IsBrowsable == true).OrderBy(x => x.Order).ToList();

        foreach (var inputDescriptor in browsableInputDescriptors)
        {
            var inputName = inputDescriptor.Name.Camelize();
            var value = activity.GetProperty(inputName);
            var wrappedInput = inputDescriptor.IsWrapped ? ToWrappedInput(value) : null;
            var syntaxProvider = wrappedInput != null ? GetSyntaxProvider(wrappedInput, inputDescriptor) : null;

            // Check if refresh is needed.
            if (inputDescriptor.UISpecifications != null
                && inputDescriptor.UISpecifications.TryGetValue("Refresh", out var refreshInput)
                && bool.Parse(refreshInput.ToString()!))
            {
                await RefreshDescriptor(activity, activityDescriptor, inputDescriptors, inputDescriptor);
            }

            var uiHintHandler = UIHintService.GetHandler(inputDescriptor.UIHint);
            object? input = inputDescriptor.IsWrapped ? wrappedInput : value;

            var context = new DisplayInputEditorContext
            {
                WorkflowDefinition = WorkflowDefinition!,
                Activity = activity,
                ActivityDescriptor = activityDescriptor,
                InputDescriptor = inputDescriptor,
                Value = input,
                SelectedExpressionDescriptor = syntaxProvider,
                UIHintHandler = uiHintHandler,
                IsReadOnly = (Workspace?.IsReadOnly ?? false) || (inputDescriptor.IsReadOnly ?? false),
            };

            context.OnValueChanged = async v => await HandleValueChangedAsync(context, v);
            var editor = uiHintHandler.DisplayInputEditor(context);
            
            models.Add(new(editor));
        }

        return models;
    }

    private async Task RefreshDescriptor(JsonObject activity, ActivityDescriptor activityDescriptor, IEnumerable<InputDescriptor> inputDescriptors, InputDescriptor currentInputDescriptor)
    {
        try
        {
            var activityTypeName = activityDescriptor.TypeName;
            var propertyName = currentInputDescriptor.Name;

            // Embed all props value in the context.
            var contextDictionary = new Dictionary<string, object>();
            foreach (var inputDescriptor in inputDescriptors)
            {
                var inputName = inputDescriptor.Name.Camelize();
                var value = activity.GetProperty(inputName);
                if (value != null)
                    contextDictionary.Add(inputName, value);
            }

            var api = await BackendApiClientProvider.GetApiAsync<IActivityDescriptorOptionsApi>();

            var result = await api.GetAsync(activityTypeName, propertyName, new()
            {
                Context = contextDictionary
            });

            currentInputDescriptor.UISpecifications = result.Items;
            
            // After refreshing, update the UI for any affected inputs
            await UpdateInputDisplayModelsAsync(activity, activityDescriptor, inputDescriptors.ToList(), currentInputDescriptor);
        }
        catch (Exception)
        {
            // If an error occurs during refresh, we still want to keep the application running
            // Errors might happen if backend is unavailable or returns an error response
            // We could log the error here or show a notification, but for now we'll just silently continue
        }
    }
    
    private bool _isRefreshingModels;
    
    private async Task UpdateInputDisplayModelsAsync(JsonObject activity, ActivityDescriptor activityDescriptor, ICollection<InputDescriptor> inputDescriptors, InputDescriptor refreshedInputDescriptor)
    {
        if (_isRefreshingModels)
            return;
        
        try
        {
            _isRefreshingModels = true;
            
            // Rebuild all models
            InputDisplayModels = (await BuildInputEditorModels(activity, activityDescriptor, inputDescriptors)).ToList();
            
            // Force UI refresh
            StateHasChanged();
        }
        finally
        {
            _isRefreshingModels = false;
        }
    }

    private static WrappedInput? ToWrappedInput(object? value)
    {
        var converterOptions = new ObjectConverterOptions(serializerOptions => { serializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase; });

        return value.ConvertTo<WrappedInput>(converterOptions);
    }

    private async Task HandleValueChangedAsync(DisplayInputEditorContext context, object? value)
    {
        var activity = context.Activity;
        var inputDescriptor = context.InputDescriptor;

        if (inputDescriptor.IsWrapped)
        {
            var wrappedInput = (WrappedInput)value!;
            var syntaxProvider = ExpressionDescriptorProvider.GetByType(wrappedInput.Expression.Type);
            context.SelectedExpressionDescriptor = syntaxProvider;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var propName = inputDescriptor.Name.Camelize();
        activity.SetProperty(value?.SerializeToNode(options), propName);

        if (OnActivityUpdated != null)
            await OnActivityUpdated(activity);
    }

    private ExpressionDescriptor? GetSyntaxProvider(WrappedInput wrappedInput, InputDescriptor inputDescriptor)
    {
        // Safely read UIHint
        var uiHint = inputDescriptor.UIHint;

        // If this is the code editor scenario and we have a DefaultSyntax, use that
        if (uiHint.Equals("code-editor", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(inputDescriptor.DefaultSyntax))
            return ExpressionDescriptorProvider.GetByType(inputDescriptor.DefaultSyntax);

        // Otherwise fall back to the wrapped expression's type—but guard null
        var exprType = wrappedInput?.Expression?.Type;
        return string.IsNullOrEmpty(exprType) ? null : ExpressionDescriptorProvider.GetByType(exprType);
    }
}