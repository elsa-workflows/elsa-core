using System;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Scripting.Liquid.Services;
using Elsa.Services;
using Elsa.Services.Models;
using Fluid;
using Fluid.Values;

namespace Elsa.Scripting.Liquid.Filters
{
    public class WorkflowDefinitionIdFilter : ILiquidFilter
    {
        private readonly IWorkflowRegistry _workflowRegistry;

        public WorkflowDefinitionIdFilter(IWorkflowRegistry workflowRegistry) => _workflowRegistry = workflowRegistry;

        public async ValueTask<FluidValue> ProcessAsync(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            var queryType = arguments.Values?.FirstOrDefault()?.ToStringValue() ?? "name";
            var queryValue = input.ToStringValue().ToLowerInvariant();

            Func<IWorkflowBlueprint, bool> predicate = queryType switch
            {
                "name" => x => string.Equals(x.Name, queryValue, StringComparison.OrdinalIgnoreCase),
                "tag" => x => string.Equals(x.Tag, queryValue, StringComparison.OrdinalIgnoreCase),
                _ => throw new ArgumentOutOfRangeException()
            };

            var workflowBlueprint = await _workflowRegistry.FindAsync(predicate);
            var workflowDefinitionId = workflowBlueprint?.Id;
            return new StringValue(workflowDefinitionId);
        }
    }
}