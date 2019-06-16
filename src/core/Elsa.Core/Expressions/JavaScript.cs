using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Models;
using Jint;

namespace Elsa.Core.Expressions
{
    public class JavaScript : IExpressionEvaluator
    {
        public static WorkflowExpression<T> CreateExpression<T>(string expression)
        {
            return new WorkflowExpression<T>(SyntaxName, expression);
        }
        
        public const string SyntaxName = "JavaScript";
        private readonly Engine engine;

        public JavaScript()
        {
            engine = new Engine(options => { options.AllowClr(); });
        }

        public string Syntax => SyntaxName;

        public Task<T> EvaluateAsync<T>(string expression, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            foreach (var variable in workflowExecutionContext.CurrentScope.Variables)
            {
                engine.SetValue(variable.Key, variable.Value);
            }

            var workflowApi = new Dictionary<string, object>
            {
                ["getArgument"] = (Func<string, object>) (name => workflowExecutionContext.Workflow.Arguments.GetVariable(name)),
                ["getVariable"] = (Func<string, object>) (name => workflowExecutionContext.CurrentScope.GetVariable(name)),
                ["getFloat"] = (Func<string, float>) (name => GetFloat(workflowExecutionContext.CurrentScope.GetVariable(name))),
                ["getInt"] = (Func<string, int>) (name => GetInt(workflowExecutionContext.CurrentScope.GetVariable(name))),
                ["getLastResult"] = (Func<object>) (() => workflowExecutionContext.CurrentScope.LastResult)
            };

            engine.SetValue("wf", workflowApi);
            engine.Execute(expression);
            
            var returnValue = engine.GetCompletionValue();

            return Task.FromResult((T) returnValue.ToObject());
        }

        private float GetFloat(object value)
        {
            if (value is float f1)
                return f1;
            
            if(value is string s)
                if (float.TryParse(s, out var f2))
                    return f2;

            return float.NaN;
        }
        
        private int GetInt(object value)
        {
            if (value is int i1)
                return i1;
            
            if(value is string s)
                if (int.TryParse(s, out var i2))
                    return i2;

            return int.MinValue;
        }
    }
}