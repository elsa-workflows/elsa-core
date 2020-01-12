// using System.Collections.Generic;
// using System.Dynamic;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;
// using Elsa.Models;
// using Elsa.Scripting.Liquid.Helpers;
// using Elsa.Scripting.Liquid.Messages;
// using Elsa.Services.Models;
// using Fluid;
// using Fluid.Values;
// using MediatR;
// using Newtonsoft.Json.Linq;
//
// namespace Elsa.Scripting.Liquid.Handlers
// {
//     public class CommonLiquidContextHandler : INotificationHandler<EvaluatingLiquidExpression>
//     {
//         static CommonLiquidContextHandler()
//         {
//             FluidValue.SetTypeMapping<ExpandoObject>(x => new ObjectValue(x));
//             FluidValue.SetTypeMapping<JObject>(o => new ObjectValue(o));
//             FluidValue.SetTypeMapping<JValue>(o => FluidValue.Create(o.Value));
//         }
//
//         public Task Handle(EvaluatingLiquidExpression notification, CancellationToken cancellationToken)
//         {
//             var context = notification.TemplateContext;
//
//             context.MemberAccessStrategy.Register<LiquidPropertyAccessor, FluidValue>((x, name) => x.GetValueAsync(name));
//             context.MemberAccessStrategy.Register<ActivityExecutionContext, FluidValue>("Input", x => ToFluidValue(x.Input));
//             context.MemberAccessStrategy.Register<ActivityExecutionContext, LiquidPropertyAccessor>("Variables", x => new LiquidPropertyAccessor(name => ToFluidValue(x.ProcessExecutionContext.GetVariables(), name)));
//             context.MemberAccessStrategy.Register<ActivityExecutionContext, LiquidObjectAccessor<IActivity>>("Activities", x => new LiquidObjectAccessor<IActivity>(name => GetActivityAsync(x, name)));
//             context.MemberAccessStrategy.Register<LiquidObjectAccessor<IActivity>, object>(GetActivityOutput);
//             context.MemberAccessStrategy.Register<LiquidObjectAccessor<object>, object>((x, name) => x.GetValueAsync(name));
//             context.MemberAccessStrategy.Register<ExpandoObject, object>((x, name) => ((IDictionary<string, object>)x)[name]);
//             context.MemberAccessStrategy.Register<JObject, object>((source, name) => source[name]);
//
//             return Task.CompletedTask;
//         }
//
//         private Task<FluidValue> ToFluidValue(Variable input) => Task.FromResult(FluidValue.Create(input?.Value));
//         
//         private Task<FluidValue> ToFluidValue(IDictionary<string, Variable> dictionary, string key) 
//             => Task.FromResult(!dictionary.ContainsKey(key) ? default : FluidValue.Create(dictionary[key].Value));
//
//         private async Task<object> GetActivityOutput(LiquidObjectAccessor<IActivity> accessor, string activityName)
//         {
//             var activity = await accessor.GetValueAsync(activityName);
//             return activity?.Output?.Value;
//         }
//
//         private Task<IActivity> GetActivityAsync(ActivityExecutionContext context, string name) 
//             => Task.FromResult(context.ProcessExecutionContext.ProcessInstance.Blueprint.Activities.FirstOrDefault(x => x.Name == name));
//     }
// }