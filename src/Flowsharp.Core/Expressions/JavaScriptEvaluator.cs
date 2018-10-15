using System;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Jint;

namespace Flowsharp.Expressions
{
    public class JavaScriptEvaluator : IExpressionEvaluator
    {
        public const string SyntaxName = "JavaScript";
        private readonly Engine engine;

        public JavaScriptEvaluator()
        {
            engine = new Engine(options => { options.AllowClr(); });
        }
        
        public string Syntax => SyntaxName;

        public Task<T> EvaluateAsync<T>(string expression, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            var workflowApi = new
            {
                getVariable = (Func<string, object>) (name => workflowExecutionContext.CurrentScope.GetVariable(name)),
                getLastResult = (Func<object>)(() => workflowExecutionContext.CurrentScope.LastResult)
            };
            
            engine.SetValue("workflow", workflowApi);
            engine.Execute(expression);
            var returnValue = engine.GetCompletionValue();

            return Task.FromResult((T)returnValue.ToObject());
        }
    }
}