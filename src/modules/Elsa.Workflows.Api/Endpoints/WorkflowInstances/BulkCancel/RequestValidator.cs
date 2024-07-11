using Elsa.Common.Models;
using FastEndpoints;
using FluentValidation;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.BulkCancel;

/// <summary>
/// Validates the request for bulk canceling workflow instances.
/// </summary>
public class RequestValidator : Validator<Request>
{
    /// <inheritdoc />
    public RequestValidator()
    {
        RuleFor(x => x.VersionOptions)
            .Must((request, options) => ValidCombination(request.DefinitionId, options))
            .WithMessage("VersionOptions and DefinitionId should either both be empty or both be not empty");
    }

    private bool ValidCombination(string? definitionId, VersionOptions? options)
    {
        return (definitionId is null && options is null)
               || (definitionId is not null && options is not null);
    }
}