using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Scripting.Liquid.Helpers;
using Elsa.Scripting.Liquid.Messages;
using Elsa.Services;
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

            context.MemberAccessStrategy.Register<LiquidPropertyAccessor, FluidValue>(
                (x, name) => x.GetValueAsync(name));
            
            context.MemberAccessStrategy.Register<ActivityExecutionContext, FluidValue>(
                "Input",
                x => ToFluidValue(x.Input));
            
            context.MemberAccessStrategy.Register<ActivityExecutionContext, LiquidPropertyAccessor>(
                "Variables",
                x => new LiquidPropertyAccessor(name => ToFluidValue(x.WorkflowExecutionContext.WorkflowInstance.Variables, name)));
            
            context.MemberAccessStrategy.Register<ActivityExecutionContext, LiquidObjectAccessor<IActivityBlueprint>>(
                "Activities",
                x => new LiquidObjectAccessor<IActivityBlueprint>(name => GetActivityAsync(x, name)));
            
            context.MemberAccessStrategy.Register<LiquidObjectAccessor<IActivity>, object?>(GetActivityOutput);
            
            context.MemberAccessStrategy.Register<LiquidObjectAccessor<object>, object>(
                (x, name) => x.GetValueAsync(name));
            
            context.MemberAccessStrategy.Register<ExpandoObject, object>(
                (x, name) => ((IDictionary<string, object>)x)[name]);
            
            context.MemberAccessStrategy.Register<JObject, object?>((source, name) => source[name]);

            return Task.CompletedTask;
        }

        private Task<FluidValue> ToFluidValue(object? input) => Task.FromResult(FluidValue.Create(input));

        private Task<FluidValue?> ToFluidValue(Variables dictionary, string key)
            => Task.FromResult(!dictionary.Has(key) ? default : FluidValue.Create(dictionary.Get(key)));

        private async Task<object?> GetActivityOutput(LiquidObjectAccessor<IActivity> accessor, string activityName)
        {
            var activity = await accessor.GetValueAsync(activityName);
            //return activity?.Output;
            throw new NotImplementedException();
        }

        private Task<IActivityBlueprint> GetActivityAsync(ActivityExecutionContext context, string name)
            => Task.FromResult(context.WorkflowExecutionContext.WorkflowBlueprint.Activities.FirstOrDefault(x => x.Name == name));
    }
}