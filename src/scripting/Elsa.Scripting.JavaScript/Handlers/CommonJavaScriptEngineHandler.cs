// using System;
// using System.Globalization;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;
// using Elsa.Scripting.JavaScript.Messages;
// using MediatR;
//
// namespace Elsa.Scripting.JavaScript.Handlers
// {
//     public class CommonJavaScriptEngineHandler : INotificationHandler<EvaluatingJavaScriptExpression>
//     {
//         public Task Handle(EvaluatingJavaScriptExpression notification, CancellationToken cancellationToken)
//         {
//             var activityContext = notification.ActivityExecutionContext;
//             var workflowContext = activityContext.ProcessExecutionContext;
//             var workflow = workflowContext.ProcessInstance;
//             var engine = notification.Engine;
//
//             engine.SetValue("input", (Func<string, object>) (name => activityContext.Input?.Value));
//             engine.SetValue("variable", (Func<string, object>) (name => workflowContext.GetVariable(name)));
//             engine.SetValue("correlationId", (Func<object>) (() => workflow.CorrelationId));
//             engine.SetValue("currentCulture", (Func<object>) (() => CultureInfo.InvariantCulture));
//             engine.SetValue("newGuid", (Func<string>) (() => Guid.NewGuid().ToString()));
//
//             var variables = workflowContext.GetVariables();
//
//             // Add workflow variables.
//             foreach (var variable in variables)
//                 engine.SetValue(variable.Key, variable.Value.Value);
//
//             // Add activity outputs.
//             foreach (var activity in workflow.Blueprint.Activities.Where(x => !string.IsNullOrWhiteSpace(x.Name) && x.Output != null)) 
//                 engine.SetValue(activity.Name, activity.Output.Value);
//
//             return Task.CompletedTask;
//         }
//     }
// }