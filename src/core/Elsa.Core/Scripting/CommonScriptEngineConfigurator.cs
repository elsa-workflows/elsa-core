using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using Elsa.Models;
using Elsa.Services.Models;
using Jint;
using Newtonsoft.Json.Linq;

namespace Elsa.Scripting
{
    public class CommonScriptEngineConfigurator : IScriptEngineConfigurator
    {
        public void Configure(Engine engine, WorkflowExecutionContext workflowExecutionContext)
        {
            var context = workflowExecutionContext;
            var workflow = workflowExecutionContext.Workflow;

            engine.SetValue("input", (Func<string, object>) (name => workflow.Input.GetVariable(name)));
            engine.SetValue("variable", (Func<string, object>) (name => context.CurrentScope.GetVariable(name)));
            engine.SetValue("lastResult", (Func<string, object>) (name => context.CurrentScope.LastResult));
            engine.SetValue("correlationId", (Func<object>) (() => context.Workflow.CorrelationId));
            engine.SetValue("currentCulture", (Func<object>) (() => CultureInfo.InvariantCulture));

            var variables = workflowExecutionContext.Workflow.Scopes.Reverse()
                .Select(x => x.Variables)
                .Aggregate(Variables.Empty, (x, y) => new Variables(x.Union(y)));
            
            foreach (var variable in variables)
            {
                // Jint causes an exception when evaluating expressions using the backtick syntax in combination with JObjects.
                // Therefore converting JObjects to ExpandoObjects, allowing expressions such as `My age is ${person.age}`.
                
                var value = variable.Value is JObject jObject
                    ? jObject.ToObject<ExpandoObject>()
                    : variable.Value;
                
                engine.SetValue(variable.Key, value);
            }

            foreach (var activity in workflowExecutionContext.Workflow.Activities.Where(x => x.Output != null))
            {
                var expando = new ExpandoObject() as IDictionary<string, object>;
                
                foreach (var variable in activity.Output)
                {
                    expando[variable.Key] = variable.Value;   
                }
                
                engine.SetValue(activity.Id, expando);
            }
        }
    }
}