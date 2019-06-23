using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Esprima.Ast;
using Jint;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Core.Expressions
{
    public class JavaScriptEvaluator : IExpressionEvaluator
    {
        public static WorkflowExpression<T> CreateExpression<T>(string expression)
        {
            return new WorkflowExpression<T>(SyntaxName, expression);
        }
        
        public const string SyntaxName = "JavaScript";
        private readonly Engine engine;

        public JavaScriptEvaluator()
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
                ["input"] = (Func<string, object>) (name => workflowExecutionContext.Workflow.Input.GetVariable(name)),
                ["variable"] = (Func<string, object>) (name => workflowExecutionContext.CurrentScope.GetVariable(name)),
                ["float"] = (Func<string, float>) (name => GetFloat(workflowExecutionContext.CurrentScope.GetVariable(name))),
                ["int"] = (Func<string, int>) (name => GetInt(workflowExecutionContext.CurrentScope.GetVariable(name))),
                ["lastResult"] = (Func<object>) (() => workflowExecutionContext.CurrentScope.LastResult)
            };

            engine.SetValue("wf", workflowApi);
            engine.Execute(expression);
            
            var returnValue = engine.GetCompletionValue();
            T result;

            if (returnValue.IsArray())
            {
                var jsArray = returnValue.AsArray();
                var elementType = typeof(T).GetElementType();
                var array = Array.CreateInstance(elementType, jsArray.Length);

                for (uint i = 0; i < jsArray.Length; i++)
                {
                    var item = jsArray[i].ToObject();
                    var convertedItem = Convert.ChangeType(item, elementType);
                    array.SetValue(convertedItem, i);
                }

                result = (T)(object)array;
            }
            else
            {
                result = (T) returnValue.ToObject();
            }
            
            return Task.FromResult(result);
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