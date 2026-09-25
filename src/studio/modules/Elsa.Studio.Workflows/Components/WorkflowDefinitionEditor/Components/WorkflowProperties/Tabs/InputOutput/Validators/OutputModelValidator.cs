using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.InputOutput.Models;
using FluentValidation;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.InputOutput.Validators;

/// <summary>
/// Represents the output model validator.
/// </summary>
public class OutputModelValidator : AbstractValidator<OutputDefinitionModel>
{
    public OutputModelValidator(WorkflowDefinition workflowDefinition, ILocalizer localizer)
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(localizer["Please enter a name for the output."]);
        
        RuleFor(x => x.Name)
            .Must((context, name, cancellationToken) =>
            {
                var existingOutput = workflowDefinition.Outputs.FirstOrDefault(x => x.Name == name);
                return existingOutput == null || existingOutput.Name == context.Name;
            })
            .WithMessage(localizer["An input with this name already exists."]);
    }
}