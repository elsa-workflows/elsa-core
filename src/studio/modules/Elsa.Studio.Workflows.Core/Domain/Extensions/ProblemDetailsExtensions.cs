using Elsa.Studio.Workflows.Domain.Models;
using Refit;

namespace Elsa.Studio.Workflows.Domain.Extensions;

/// <summary>
/// Provides extension methods for <see cref="ProblemDetails"/>.
/// </summary>
public static class ProblemDetailsExtensions
{
    /// <summary>
    /// Converts a <see cref="ProblemDetails"/> object to a <see cref="ValidationErrors"/> object.
    /// </summary>
    public static ValidationErrors ToValidationErrors(this ProblemDetails problemDetails)
    {
        var validationErrors =
            from pair in problemDetails.Errors
            from message in pair.Value
            select new ValidationError(message);
        return new(validationErrors.ToList());
    }
}