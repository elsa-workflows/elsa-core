using System;
using Elsa.Scripting;
using Elsa.Services.Models;
using Jint;

namespace Elsa.Core.Scripting
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
            
            foreach (var variable in workflowExecutionContext.CurrentScope.Variables)
            {
                engine.SetValue(variable.Key, variable.Value);
            }
        }
    }
}