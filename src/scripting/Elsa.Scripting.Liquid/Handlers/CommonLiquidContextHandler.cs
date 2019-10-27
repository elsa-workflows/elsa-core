using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Scripting.Liquid.Helpers;
using Elsa.Scripting.Liquid.Messages;
using Elsa.Services.Models;
using Fluid;
using Fluid.Values;
using MediatR;

namespace Elsa.Scripting.Liquid.Handlers
{
    public class CommonLiquidContextHandler : INotificationHandler<EvaluatingLiquidExpression>
    {
        public Task Handle(EvaluatingLiquidExpression notification, CancellationToken cancellationToken)
        {
            var context = notification.TemplateContext;
            context.MemberAccessStrategy.Register<LiquidPropertyAccessor, FluidValue>((x, name) => x.GetValueAsync(name));
            context.MemberAccessStrategy.Register<WorkflowExecutionContext, LiquidPropertyAccessor>("Input", x => new LiquidPropertyAccessor(name => ToFluidValue(x.Workflow.Input, name)));
            context.MemberAccessStrategy.Register<WorkflowExecutionContext, LiquidPropertyAccessor>("Output", x => new LiquidPropertyAccessor(name => ToFluidValue(x.Workflow.Output, name)));
            context.MemberAccessStrategy.Register<WorkflowExecutionContext, LiquidPropertyAccessor>("Variables", x => new LiquidPropertyAccessor(name => ToFluidValue(x.GetVariables(), name)));
            context.MemberAccessStrategy.Register<WorkflowExecutionContext, LiquidObjectAccessor<IActivity>>("Activities", x => new LiquidObjectAccessor<IActivity>(id => GetActivityAsync(x, id)));
            context.MemberAccessStrategy.Register<LiquidObjectAccessor<IActivity>, LiquidPropertyAccessor>((x, activityId) => new LiquidPropertyAccessor(outputKey => ToFluidValue(x, activityId, outputKey)));
            
            return Task.CompletedTask;
        }
        
        private Task<FluidValue> ToFluidValue(IDictionary<string, object> dictionary, string key)
        {
            return Task.FromResult(!dictionary.ContainsKey(key) ? default : FluidValue.Create(dictionary[key]));
        }

        private async Task<FluidValue> ToFluidValue(LiquidObjectAccessor<IActivity> accessor, string activityName, string outputKey)
        {
            var activity = await accessor.GetValueAsync(activityName);
            return await ToFluidValue(activity.Output, outputKey);
        }

        private Task<IActivity> GetActivityAsync(WorkflowExecutionContext executionContext, string id) => Task.FromResult(executionContext.Workflow.Activities.FirstOrDefault(x => x.Id == id));
    }
}