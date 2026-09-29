using Elsa.Agents;
using FluentValidation;
using Microsoft.AspNetCore.Components.Forms;

namespace Elsa.Studio.Agents.UI.Validators;

/// <summary>Awaits name validation before submission and discards stale or disposed results.</summary>
internal sealed class AgentSubmitValidator : IDisposable
{
    private readonly IValidator<AgentInputModel> _validator;
    private readonly AgentInputModel _model;
    private readonly EditContext _editContext;
    private readonly ValidationMessageStore _messages;
    private long _revision;
    private bool _disposed;

    public AgentSubmitValidator(IValidator<AgentInputModel> validator, AgentInputModel model, EditContext editContext)
    {
        _validator = validator;
        _model = model;
        _editContext = editContext;
        _messages = new(editContext);
        _editContext.OnFieldChanged += OnFieldChanged;
    }

    public async Task<bool> ValidateAndPublishAsync()
    {
        if (_disposed)
        {
            return false;
        }

        var revision = ++_revision;
        var name = _model.Name;
        var result = await _validator.ValidateAsync(_model);
        if (_disposed || revision != _revision || !string.Equals(name, _model.Name, StringComparison.Ordinal))
        {
            return false;
        }

        _messages.Clear();
        foreach (var error in result.Errors)
        {
            _messages.Add(new FieldIdentifier(_model, error.PropertyName), error.ErrorMessage);
        }

        _editContext.NotifyValidationStateChanged();
        return result.IsValid;
    }

    private void OnFieldChanged(object? sender, FieldChangedEventArgs args)
    {
        ++_revision;
        _messages.Clear(args.FieldIdentifier);
        _editContext.NotifyValidationStateChanged();
    }

    public void Dispose()
    {
        _disposed = true;
        _editContext.OnFieldChanged -= OnFieldChanged;
    }
}
