using Elsa.Models;

namespace Elsa.Liquid.Helpers
{
    public record LiquidActivityModel(ActivityExecutionContext ActivityExecutionContext, string? ActivityName, string? ActivityId)
    {
    }
}