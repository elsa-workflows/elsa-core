using Elsa.Models;
using Elsa.Persistence;
using Elsa.Scripting.Liquid.Services;
using Elsa.Services;

using Fluid;
using Fluid.Values;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Elsa.Scripting.Liquid.Filters
{
    public class WorkflowDefinitionIdFilter : ILiquidFilter
    {
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;

        public WorkflowDefinitionIdFilter(IWorkflowRegistry workflowRegistry, IWorkflowInstanceStore workflowInstanceStore)
        {
            _workflowRegistry = workflowRegistry;
            _workflowInstanceStore = workflowInstanceStore;
        }
        public async ValueTask<FluidValue> ProcessAsync(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            var queryType = arguments.Values?.FirstOrDefault()?.ToStringValue() ?? "name";
            string? tenantId = null;
            if (arguments.Values?.Count() > 1)
            {
                var wfInstanceId = arguments.Values?.LastOrDefault().ToStringValue();
                if (wfInstanceId is not null)
                {
                    var wfInstance = await _workflowInstanceStore.FindByIdAsync(wfInstanceId);
                    tenantId = wfInstance?.TenantId;
                }
            }
            var queryValue = input.ToStringValue();

            var task = queryType switch
            {
                "name" => GetWorkflowDefinitionIdByName(queryValue, tenantId),
                "tag" => GetWorkflowDefinitionIdByTag(queryValue, tenantId),
                _ => throw new ArgumentOutOfRangeException()
            };

            var workflowDefinitionId = await task;
            return new StringValue(workflowDefinitionId);
        }

        private async Task<string?> GetWorkflowDefinitionIdByTag(string tag, string? tenantId = null)
        {
            var workflowBlueprint = await _workflowRegistry.FindByTagAsync(tag, VersionOptions.Published, tenantId: tenantId);
            return workflowBlueprint?.Id;
        }

        private async Task<string?> GetWorkflowDefinitionIdByName(string name, string? tenantId = null)
        {
            var workflowBlueprint = await _workflowRegistry.FindByNameAsync(name, VersionOptions.Published, tenantId: tenantId);
            return workflowBlueprint?.Id;
        }
    }
}