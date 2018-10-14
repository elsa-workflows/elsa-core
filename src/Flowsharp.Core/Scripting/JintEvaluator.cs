using System;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Jint;

namespace Flowsharp.Scripting
{
    public class JintEvaluator : IScriptEvaluator
    {
        private readonly Engine engine;

        public JintEvaluator()
        {
            engine = new Engine(options => { options.AllowClr(); });
        }
        
        public Task<T> EvaluateAsync<T>(ScriptExpression<T> script, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            engine.SetValue("workflow.getVariable", (Func<string, object>)(name => workflowExecutionContext.CurrentScope.GetVariable(name)));
            engine.Execute(script.Expression);
            var returnValue = engine.GetCompletionValue();

            return Task.FromResult((T)returnValue.ToObject());
        }
    }
}