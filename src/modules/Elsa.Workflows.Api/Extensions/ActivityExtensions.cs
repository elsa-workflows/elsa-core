using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;

namespace Elsa.Extensions;

public static class ActivityExtensions
{
    public static bool IsFlowchart(this IActivity activity) => activity is Workflow;
}