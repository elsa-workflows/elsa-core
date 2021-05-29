using Elsa.Services.Models;

namespace Elsa.Scripting.Liquid.Helpers
{
    public record LiquidActivityModel(ActivityExecutionContext ActivityExecutionContext, string? ActivityName, string? ActivityId)
    {
    }
}