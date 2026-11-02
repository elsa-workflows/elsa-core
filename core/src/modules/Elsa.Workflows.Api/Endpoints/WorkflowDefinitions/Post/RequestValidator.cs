using Elsa.Workflows.Management.Models;
using FastEndpoints;
using FluentValidation;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Post;

internal sealed class RequestValidator : Validator<SaveWorkflowDefinitionRequest>
{
    public RequestValidator()
    {
        RuleFor(request => request.Model).NotNull().OverridePropertyName("model");
        RuleFor(request => request.Model.Name)
            .NotEmpty()
            .WithMessage("Workflow name is required.")
            .OverridePropertyName("name")
            .When(request => request.Model != null);
    }
}
