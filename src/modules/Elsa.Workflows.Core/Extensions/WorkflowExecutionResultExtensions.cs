using Elsa.Extensions;
using Elsa.Workflows.Models;

namespace Elsa.Workflows;

public static class WorkflowExecutionResultExtensions
{
    public static T GetActivityOutput<T>(this RunWorkflowResult result, IActivity activity, string? outputName = null)
    {
        var value = result.WorkflowExecutionContext.GetOutputByActivityId(activity.Id, outputName);

        // If the value is already of the requested type, return it directly.
        if (value is T tValue)
            return tValue;

        // Handle nulls for reference/nullable types.
        if (value is null)
        {
            // If T is a reference type or nullable, default(T) is fine.
            // Otherwise, this is a runtime error.
            return default(T) is null ? default! : throw new InvalidCastException($"Cannot convert null to non-nullable type {typeof(T).FullName}.");
        }

        // Try to convert using System.Convert when possible (handles numeric casts like Double -> Int32).
        try
        {
            var targetType = typeof(T);

            // Unwrap nullable<T> to its underlying type for conversion.
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            var converted = Convert.ChangeType(value, underlyingType, System.Globalization.CultureInfo.InvariantCulture);

            // If T is nullable and we converted to the underlying type, just cast.
            return (T)converted!;
        }
        catch (Exception ex)
        {
            throw new InvalidCastException($"Unable to convert value of type '{value.GetType().FullName}' to type '{typeof(T).FullName}'.", ex);
        }
    }
}