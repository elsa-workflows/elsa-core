using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;

namespace Elsa.Alterations.Endpoints.Alterations;

internal static class TimestampFilterValidation
{
    public static bool Validate(IEnumerable<TimestampFilter>? timestampFilters, Action<string> addError)
    {
        var isValid = true;

        foreach (var error in WorkflowInstanceFilter.ValidateTimestampFilters(timestampFilters))
        {
            addError(error);
            isValid = false;
        }

        return isValid;
    }
}
