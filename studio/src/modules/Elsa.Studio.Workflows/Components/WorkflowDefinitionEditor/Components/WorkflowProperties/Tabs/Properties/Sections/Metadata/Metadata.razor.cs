using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.Validators;
using FluentValidation.Results;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.Properties.Sections.Metadata;

/// <summary>
/// A component that renders the workflow definition metadata.
/// </summary>
public partial class Metadata
{
    private readonly WorkflowMetadataModel _model = new();
    private WorkflowPropertiesModelValidator _validator = default!;
    private EditContext _editContext = default!;
    private ValidationMessageStore _validationMessages = default!;
    private WorkflowDefinition? _boundWorkflowDefinition;
    private string? _lastLoadedName;
    private string? _lastLoadedDescription;
    private string? _lastCommittedName;
    private string? _lastCommittedDescription;
    private long _editVersion;
    private long _workflowVersion;
    private long _validationVersion;
    private bool _hasCommittedMetadata;
    private long _lastWorkflowDefinitionReloadVersion;

    /// <summary>
    /// Gets or sets the workflow definition.
    /// </summary>
    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the callback that is invoked when the workflow definition is updated.
    /// </summary>
    [Parameter] public EventCallback WorkflowDefinitionUpdated { get; set; }
    [CascadingParameter] private IWorkspace? Workspace { get; set; }
    [CascadingParameter(Name = "WorkflowDefinitionReloadVersion")]
    private long WorkflowDefinitionReloadVersion { get; set; }
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = null!;
    
    private bool IsReadOnly => Workspace?.IsReadOnly ?? false;

    /// <inheritdoc />
    protected override void OnInitialized() => _validator = new(WorkflowDefinitionService, Localizer);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        var isExplicitReload = WorkflowDefinitionReloadVersion != _lastWorkflowDefinitionReloadVersion;
        _lastWorkflowDefinitionReloadVersion = WorkflowDefinitionReloadVersion;

        if (!isExplicitReload && ReferenceEquals(_boundWorkflowDefinition, WorkflowDefinition))
            return;

        var isSameWorkflowVersion = IsSameWorkflowVersion(_boundWorkflowDefinition, WorkflowDefinition);

        // A save response creates a new object for the same workflow version. Keep local edits and
        // committed metadata across same-version responses, and use a different version as the
        // explicit reload boundary.
        if (!isExplicitReload && isSameWorkflowVersion && (HasLocalChanges || _hasCommittedMetadata))
        {
            // The editor can receive an older save response after this form has already committed a
            // newer local value. Keep the parent object aligned with the last validated submission;
            // later edits may still be unvalidated.
            if (_hasCommittedMetadata && !HasMatchingCommittedMetadata(WorkflowDefinition))
            {
                WorkflowDefinition.Name = _lastCommittedName;
                WorkflowDefinition.Description = _lastCommittedDescription;
            }

            _boundWorkflowDefinition = WorkflowDefinition;
            _lastLoadedName = WorkflowDefinition.Name;
            _lastLoadedDescription = WorkflowDefinition.Description;

            return;
        }

        _boundWorkflowDefinition = WorkflowDefinition;
        _model.DefinitionId = WorkflowDefinition.DefinitionId;
        _model.Description = WorkflowDefinition.Description;
        _model.Name = WorkflowDefinition.Name;
        _editContext = new EditContext(_model);
        _validationMessages = new ValidationMessageStore(_editContext);
        _lastLoadedName = WorkflowDefinition.Name;
        _lastLoadedDescription = WorkflowDefinition.Description;
        _editVersion++;
        _workflowVersion++;
        _validationVersion++;
        _hasCommittedMetadata = false;
        _lastCommittedName = null;
        _lastCommittedDescription = null;
    }

    private Task ValidateForm() => ValidateAndCommitAsync();

    private Task OnSubmit(EditContext _) => ValidateAndCommitAsync();

    private async Task ValidateAndCommitAsync()
    {
        var workflowVersion = _workflowVersion;
        var editVersion = _editVersion;
        var workflowDefinition = WorkflowDefinition;
        var validationVersion = ++_validationVersion;

        var result = await _validator.ValidateAsync(_model);

        if (validationVersion != _validationVersion ||
            workflowVersion != _workflowVersion || editVersion != _editVersion ||
            !IsSameWorkflowVersion(workflowDefinition, WorkflowDefinition))
            return;

        PublishValidationResult(result);

        if (!result.IsValid)
            return;

        await CommitAsync(workflowVersion, editVersion, workflowDefinition);
    }

    private Task OnNameChanged(string value)
    {
        if (string.Equals(_model.Name, value, StringComparison.Ordinal))
            return Task.CompletedTask;

        _model.Name = value;
        _editVersion++;
        _validationVersion++;
        ClearValidationMessages(nameof(WorkflowMetadataModel.Name));
        return Task.CompletedTask;
    }

    private Task OnDescriptionChanged(string value)
    {
        if (string.Equals(_model.Description, value, StringComparison.Ordinal))
            return Task.CompletedTask;

        _model.Description = value;
        _editVersion++;
        _validationVersion++;
        ClearValidationMessages(nameof(WorkflowMetadataModel.Description));
        return Task.CompletedTask;
    }

    private async Task CommitAsync(long workflowVersion, long editVersion, WorkflowDefinition workflowDefinition)
    {
        if (workflowVersion != _workflowVersion || editVersion != _editVersion ||
            !IsSameWorkflowVersion(workflowDefinition, WorkflowDefinition))
            return;

        WorkflowDefinition.Description = _model.Description;
        WorkflowDefinition.Name = _model.Name;
        _lastLoadedName = _model.Name;
        _lastLoadedDescription = _model.Description;
        _lastCommittedName = _model.Name;
        _lastCommittedDescription = _model.Description;
        _hasCommittedMetadata = true;

        if (WorkflowDefinitionUpdated.HasDelegate)
            await WorkflowDefinitionUpdated.InvokeAsync();
    }

    private bool HasLocalChanges => !string.Equals(_model.Name, _lastLoadedName, StringComparison.Ordinal) ||
                                    !string.Equals(_model.Description, _lastLoadedDescription, StringComparison.Ordinal);

    private void ClearValidationMessages(string fieldName)
    {
        _validationMessages.Clear(new FieldIdentifier(_model, fieldName));
        _editContext.NotifyValidationStateChanged();
    }

    private void PublishValidationResult(ValidationResult result)
    {
        _validationMessages.Clear();

        foreach (var error in result.Errors)
        {
            var fieldIdentifier = new FieldIdentifier(_model, error.PropertyName);
            _validationMessages.Add(fieldIdentifier, error.ErrorMessage);
        }

        _editContext.NotifyValidationStateChanged();
    }

    private bool HasMatchingCommittedMetadata(WorkflowDefinition definition) =>
        _hasCommittedMetadata &&
        string.Equals(_lastCommittedName, definition.Name, StringComparison.Ordinal) &&
        string.Equals(_lastCommittedDescription, definition.Description, StringComparison.Ordinal);

    private static bool IsSameWorkflowVersion(WorkflowDefinition? first, WorkflowDefinition? second) =>
        first != null && second != null &&
        string.Equals(first.DefinitionId, second.DefinitionId, StringComparison.Ordinal) &&
        string.Equals(first.Id, second.Id, StringComparison.Ordinal);
}
