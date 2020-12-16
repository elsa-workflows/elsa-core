using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Scripting.Liquid.Helpers;
using Elsa.Scripting.Liquid.Messages;
using Elsa.Services.Models;
using Fluid;
using Fluid.Values;
using MediatR;
using Newtonsoft.Json.Linq;

namespace Elsa.Scripting.Liquid.Handlers
{
    public class CommonLiquidContextHandler : INotificationHandler<EvaluatingLiquidExpression>
    {
        static CommonLiquidContextHandler()
        {
            FluidValue.SetTypeMapping<ExpandoObject>(x => new ObjectValue(x));
            FluidValue.SetTypeMapping<JObject>(o => new ObjectValue(o));
            FluidValue.SetTypeMapping<JValue>(o => FluidValue.Create(o.Value));
        }

        public Task Handle(EvaluatingLiquidExpression notification, CancellationToken cancellationToken)
        {
            var context = notification.TemplateContext;

            context.MemberAccessStrategy.Register<LiquidPropertyAccessor, FluidValue>((x, name) => x.GetValueAsync(name));
            context.MemberAccessStrategy.Register<WorkflowExecutionContext, LiquidPropertyAccessor>("Input", x => new LiquidPropertyAccessor(name => ToFluidValue(x.Workflow.Input, name)));
            context.MemberAccessStrategy.Register<WorkflowExecutionContext, LiquidPropertyAccessor>("Output", x => new LiquidPropertyAccessor(name => ToFluidValue(x.Workflow.Output, name)));
            context.MemberAccessStrategy.Register<WorkflowExecutionContext, LiquidPropertyAccessor>("Variables", x => new LiquidPropertyAccessor(name => ToFluidValue(x.GetVariables(), name)));
            context.MemberAccessStrategy.Register<WorkflowExecutionContext, LiquidPropertyAccessor>("TransientState", x => new LiquidPropertyAccessor(name => ToFluidValue(x.TransientState, name)));
            context.MemberAccessStrategy.Register<WorkflowExecutionContext, LiquidObjectAccessor<IActivity>>("Activities", x => new LiquidObjectAccessor<IActivity>(name => GetActivityAsync(x, name)));
            context.MemberAccessStrategy.Register<LiquidObjectAccessor<IActivity>, LiquidObjectAccessor<object>>((x, activityName) => new LiquidObjectAccessor<object>(outputKey => GetActivityOutput(x, activityName, outputKey)));
            context.MemberAccessStrategy.Register<LiquidObjectAccessor<object>, object>((x, name) => x.GetValueAsync(name));
            context.MemberAccessStrategy.Register<ExpandoObject, object>((x, name) => ((IDictionary<string, object>)x)[name]);
            context.MemberAccessStrategy.Register<JObject, object>((source, name) => source[name]);

            return Task.CompletedTask;
        }

        private Task<FluidValue> ToFluidValue(IDictionary<string, Variable> dictionary, string key) 
            => Task.FromResult(!dictionary.TryGetValue(key, out var variable) ? default : FluidValue.Create(variable.Value));

        private async Task<object> GetActivityOutput(LiquidObjectAccessor<IActivity> accessor, string activityName, string outputKey)
        {
            var activity = await accessor.GetValueAsync(activityName);
            var output = activity.Output;
            return output.GetVariable(outputKey);
        }

        private Task<IActivity> GetActivityAsync(WorkflowExecutionContext executionContext, string name) 
            => Task.FromResult(executionContext.Workflow.Activities.FirstOrDefault(x => x.Name == name));
    }
}