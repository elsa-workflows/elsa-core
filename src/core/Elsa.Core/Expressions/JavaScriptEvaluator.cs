using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            return (T)ConvertValue(value, typeof(T));
        }

        private object ConvertValue(JsValue value, Type targetType)
        {
            if (value.IsNull())
                return default;

            if (value.IsBoolean())
                return value.AsBoolean();

            if (value.IsDate())
                return value.AsDate().ToDateTime();

            if (value.IsNumber())
                return value.AsNumber();

            if (value.IsString())
                return value.AsString();

            if (value.IsObject())
                return value.AsObject().ToObject();

            if (value.IsArray())
            {
                var arrayInstance = value.AsArray();
                var elementType = targetType.GetElementType();
                var array = Array.CreateInstance(elementType, arrayInstance.Length);

                for (uint i = 0; i < array.Length; i++)
                {
                    var jsValue = arrayInstance[i];
                    var convertedValue = ConvertValue(jsValue, elementType);
                    array.SetValue(convertedValue, i);
                }

                return array;
            }

            throw new ArgumentException($"Value type {value.Type} is not supported.", nameof(value));
        }
    }
}