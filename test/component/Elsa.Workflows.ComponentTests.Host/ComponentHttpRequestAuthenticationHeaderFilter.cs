using Elsa.Http;
using Elsa.Workflows;

namespace Elsa.Workflows.ComponentTests.Host;

/// <summary>
/// Masks authorization input persisted by Send HTTP Request activities.
/// </summary>
internal sealed class ComponentHttpRequestAuthenticationHeaderFilter : ActivityStateFilterBase
{
    protected override ActivityStateFilterResult OnExecute(ActivityStateFilterContext context)
    {
        var activity = context.ActivityExecutionContext.Activity;
        var inputDescriptor = context.InputDescriptor;

        if (activity is not SendHttpRequestBase || inputDescriptor.Name is not nameof(SendHttpRequestBase.Authorization))
            return ActivityStateFilterResult.Pass();

        var value = context.Value.GetString();
        return value is null
            ? ActivityStateFilterResult.Pass()
            : Filtered(new string('*', value.Length));
    }
}
