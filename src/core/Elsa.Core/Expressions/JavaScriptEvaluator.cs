using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Scripting;
using Elsa.Services;
using Elsa.Services.Models;
using Esprima.Ast;
using Jint;
using Jint.Native;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Core.Expressions
{
    public class JavaScriptEvaluator : IExpressionEvaluator
    {
        private readonly IEnumerable<IScriptEngineConfigurator> configurators;
        public const string SyntaxName = "JavaScript";

        public static WorkflowExpression<T> CreateExpression<T>(string expression)
        {
            return new WorkflowExpression<T>(SyntaxName, expression);
        }

        public JavaScriptEvaluator(IEnumerable<IScriptEngineConfigurator> configurators)
        {
            this.configurators = configurators;
        }
        
        public string Syntax => SyntaxName;

        public Task<T> EvaluateAsync<T>(string expression, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            var engine = new Engine(options => { options.AllowClr(); });
            
            ConfigureEngine(engine, workflowExecutionContext);
            engine.Execute(expression);
            
            var result = ConvertValue<T>(engine.GetCompletionValue());
            
            return Task.FromResult(result);
        }

        private void ConfigureEngine(Engine engine, WorkflowExecutionContext workflowExecutionContext)
        {
            foreach (var configurator in configurators)
            {
                configurator.Configure(engine, workflowExecutionContext);
            }
        }

        private T ConvertValue<T>(JsValue value)
        {
            if (value.IsArray())
            {
                var jsArray = value.AsArray();
                var elementType = typeof(T).GetElementType();
                var array = Array.CreateInstance(elementType, jsArray.Length);

                for (uint i = 0; i < jsArray.Length; i++)
                {
                    var item = jsArray[i].ToObject();
                    var convertedItem = Convert.ChangeType(item, elementType);
                    array.SetValue(convertedItem, i);
                }

                return (T)(object)array;
            }

            return (T) value.ToObject();
        }
    }
}