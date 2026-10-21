using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Activities.Compensation;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions;

internal static class CompensationDraftPublishFailure
{
    public static async Task<bool> TrySendAsync(HttpContext context, IEnumerable<WorkflowValidationError> validationErrors, CancellationToken cancellationToken)
    {
        var errors = validationErrors.ToList();
        var diagnostics = errors.Where(error => error.Code == CompensationScope.DraftOnlyDiagnosticCode).ToList();
        if (diagnostics.Count == 0)
            return false;

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new
        {
            errors = new Dictionary<string, string[]> { ["generalErrors"] = errors.Select(error => error.Message).ToArray() },
            code = CompensationScope.DraftOnlyDiagnosticCode,
            data = new { diagnostics = diagnostics.Select(error => error.Data).ToArray() }
        }, cancellationToken);
        return true;
    }
}
