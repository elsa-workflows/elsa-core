using System.Linq;
using System.Threading.Tasks;
using Elsa.Providers.WorkflowStorage;
using Elsa.Scripting.Liquid.Helpers;
using Elsa.Scripting.Liquid.Services;
using Elsa.Services.Models;
using Elsa.Services.WorkflowStorage;
using Fluid;
using Fluid.Values;

namespace Elsa.Scripting.Liquid.Filters
{
    public class ActivityOutputFilter : ILiquidFilter
    {
        private readonly IWorkflowStorageService _workflowStorageService;

        public ActivityOutputFilter(IWorkflowStorageService workflowStorageService)
        {
            _workflowStorageService = workflowStorageService;
        }
        
        public async ValueTask<FluidValue> ProcessAsync(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            var activityName = input.ToStringValue();
            var activityPropertyName = arguments.Values.First().ToStringValue();
            var activityExecutionContext = (ActivityExecutionContext) context.Model;
            var activityBlueprint = activityExecutionContext.WorkflowExecutionContext.GetActivityBlueprintByName(activityName)!;
            var activityId = activityBlueprint.Id;
            var storageProviderName = activityBlueprint.PropertyStorageProviders.GetItem(activityPropertyName);
            var storageContext = new WorkflowStorageContext(activityExecutionContext.WorkflowExecutionContext.WorkflowInstance, activityId);
            var value = await _workflowStorageService.LoadAsync(storageProviderName, storageContext, activityPropertyName);
            return new ObjectValue(value);
        }
    }
}