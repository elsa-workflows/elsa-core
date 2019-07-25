using System;
using System.Dynamic;
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
            engine.SetValue("correlationId", (Func<string, object>) (name => context.Workflow.CorrelationId));

            foreach (var variable in workflowExecutionContext.CurrentScope.Variables)
            {
                engine.SetValue(variable.Key, variable.Value);
            }

            foreach (var activity in workflowExecutionContext.Workflow.Activities)
            {
                engine.SetValue(
                    activity.Id,
                    new
                    {
                        state = activity.State,
                        output = JsonConvert.DeserializeObject<ExpandoObject>(activity.Output.ToString())
                    }
                );
            }
        }
    }
}