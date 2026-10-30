using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.Variables.Models;
using FluentValidation;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.Variables.Validators;

/// <summary>
/// Validates a variable model.
/// </summary>
public class VariableModelValidator : AbstractValidator<VariableModel>
{
    public VariableModelValidator(WorkflowDefinition workflowDefinition, ILocalizer localizer)
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(localizer["Please enter a name for the variable."]);
        
        RuleFor(x => x.Name)
            .Must((context, name, cancellationToken) =>
            {
                var existingVariable = workflowDefinition.Variables.FirstOrDefault(x => x.Name == name && x.Id != context.Id);
                return existingVariable == null || existingVariable.Id == context.Name;
            })
            .WithMessage(localizer["A variable with this name already exists in the current scope."]);
    }
}