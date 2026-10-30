using Elsa.Studio.Workflows.Models;
using FluentValidation;
using Microsoft.AspNetCore.Components.Forms;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionList;

/// <summary>
/// Validates a <see cref="WorkflowMetadataModel"/> directly against its validator and publishes the result to
/// its own message store. This keeps the submit decision independent of Blazilla's shared, unversioned
/// field-change validation, which can otherwise overwrite the message store with a stale answer from an
/// earlier asynchronous uniqueness check.
/// </summary>
/// <remarks>
/// Owns the message store's subscription to the edit context's <see cref="EditContext.OnFieldChanged"/>
/// event, so that a duplicate-name message this instance published does not linger once the user edits the
/// field it was published against. Dispose the instance when its edit context is discarded (e.g. because the
/// owning dialog rebuilds it in <c>OnParametersSet</c>) so its handler does not keep running against a
/// replaced context.
/// </remarks>
internal sealed class WorkflowMetadataSubmitValidator : IDisposable
{
    private readonly IValidator<WorkflowMetadataModel> _validator;
    private readonly WorkflowMetadataModel _model;
    private readonly EditContext _editContext;
    private readonly ValidationMessageStore _validationMessages;

    public WorkflowMetadataSubmitValidator(IValidator<WorkflowMetadataModel> validator, WorkflowMetadataModel model, EditContext editContext)
    {
        _validator = validator;
        _model = model;
        _editContext = editContext;
        _validationMessages = new(editContext);
        _editContext.OnFieldChanged += OnFieldChanged;
    }

    /// <summary>
    /// Validates the model and publishes the result to this instance's message store, unless the bound name
    /// changed while the (asynchronous) validation was in flight. In that case the result no longer describes
    /// the current value, so it is discarded without being published or acted on; the caller must treat this
    /// as "not submitted" and let the user's next submission re-validate the current value.
    /// </summary>
    public async Task<bool> ValidateAndPublishAsync()
    {
        var submittedName = _model.Name;

        var result = await _validator.ValidateAsync(_model);

        if (!string.Equals(_model.Name, submittedName, StringComparison.Ordinal))
            return false;

        _validationMessages.Clear();

        foreach (var error in result.Errors)
            _validationMessages.Add(new FieldIdentifier(_model, error.PropertyName), error.ErrorMessage);

        _editContext.NotifyValidationStateChanged();

        return result.IsValid;
    }

    // Blazilla's field-change validation clears only its own message store, so a duplicate-name message
    // this instance published would otherwise remain after the user corrects the field.
    private void OnFieldChanged(object? sender, FieldChangedEventArgs e)
    {
        _validationMessages.Clear(e.FieldIdentifier);
        _editContext.NotifyValidationStateChanged();
    }

    /// <inheritdoc />
    public void Dispose() => _editContext.OnFieldChanged -= OnFieldChanged;
}
