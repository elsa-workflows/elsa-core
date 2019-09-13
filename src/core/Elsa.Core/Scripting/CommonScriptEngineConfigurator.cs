using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Elsa.Activities.ControlFlow;
using Elsa.Models;
using Elsa.Services.Models;
using Jint;
using Newtonsoft.Json;

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

            foreach (var variable in workflowExecutionContext.CurrentScope.Variables)
            {
                engine.SetValue(variable.Key, variable.Value);
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