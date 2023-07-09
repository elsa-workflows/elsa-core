using Elsa.Workflows.Core;

namespace Elsa.Liquid.Helpers;

public record LiquidActivityModel(ActivityExecutionContext ActivityExecutionContext, string? ActivityName, string? ActivityId)
{
}