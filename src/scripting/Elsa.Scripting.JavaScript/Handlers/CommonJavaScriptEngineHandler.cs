using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Scripting.JavaScript.Messages;
using MediatR;
using Newtonsoft.Json.Linq;

namespace Elsa.Scripting.JavaScript.Handlers
{
    public class CommonJavaScriptEngineHandler : INotificationHandler<EvaluatingJavaScriptExpression>
    {
        public Task Handle(EvaluatingJavaScriptExpression notification, CancellationToken cancellationToken)
        {
            var executionContext = notification.WorkflowExecutionContext;
            var workflow = executionContext.Workflow;
            var engine = notification.Engine;

            engine.SetValue("input", (Func<string, object>) (name => workflow.Input.GetVariable(name)));
            engine.SetValue("variable", (Func<string, object>) (name => executionContext.CurrentScope.GetVariable(name)));
            engine.SetValue("lastResult", (Func<string, object>) (name => executionContext.CurrentScope.LastResult));
            engine.SetValue("correlationId", (Func<object>) (() => executionContext.Workflow.CorrelationId));
            engine.SetValue("currentCulture", (Func<object>) (() => CultureInfo.InvariantCulture));

            var variables = executionContext.GetVariables();
            
            foreach (var variable in variables)
            {
                // Jint causes an exception when evaluating expressions using the backtick syntax in combination with JObjects.
                // Therefore converting JObjects to ExpandoObjects, allowing expressions such as `My age is ${person.age}`.
                
                var value = variable.Value is JObject jObject
                    ? jObject.ToObject<ExpandoObject>()
                    : variable.Value;
                
                engine.SetValue(variable.Key, value);
            }

            foreach (var activity in executionContext.Workflow.Activities.Where(x => !string.IsNullOrWhiteSpace(x.Name) && x.Output != null))
            {
                var expando = new ExpandoObject() as IDictionary<string, object>;
                
                foreach (var variable in activity.Output)
                {
                    expando[variable.Key] = variable.Value;   
                }
                
                engine.SetValue(activity.Name, expando);
            }
            
            return Task.CompletedTask;
        }
    }
}