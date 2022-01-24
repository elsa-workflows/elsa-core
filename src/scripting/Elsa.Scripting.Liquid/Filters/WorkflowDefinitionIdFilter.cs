using System;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Scripting.Liquid.Services;
using Elsa.Services;
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

            var task = queryType switch
            {
                "name" => GetWorkflowDefinitionIdByName(queryValue) ,
                "tag" => GetWorkflowDefinitionIdByTag(queryValue),
                _ => throw new ArgumentOutOfRangeException()
            };

            var workflowDefinitionId = await task;
            return new StringValue(workflowDefinitionId);
        }
        
        private async Task<string?> GetWorkflowDefinitionIdByTag(string tag)
        {
            var workflowBlueprint = await _workflowRegistry.FindByTagAsync(tag, VersionOptions.Published);
            return workflowBlueprint?.Id;
        }

        private async Task<string?> GetWorkflowDefinitionIdByName(string name)
        {
            var workflowBlueprint = await _workflowRegistry.FindByNameAsync(name, VersionOptions.Published);
            return workflowBlueprint?.Id;
        }
    }
}