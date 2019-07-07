using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Scripting;
using Elsa.Services;
using Elsa.Services.Models;
using Jint;
using Jint.Native;
using Newtonsoft.Json;

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

        public Task<object> EvaluateAsync(string expression, Type type, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            var engine = new Engine(options => { options.AllowClr(); });

            ConfigureEngine(engine, workflowExecutionContext);
            engine.Execute(expression);

            var result = ConvertValue(engine.GetCompletionValue(), type);

            return Task.FromResult(result);
        }

        private void ConfigureEngine(Engine engine, WorkflowExecutionContext workflowExecutionContext)
        {
            foreach (var configurator in configurators)
            {
                configurator.Configure(engine, workflowExecutionContext);
            }
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
            {
                var obj = value.AsObject().ToObject();
                var json = JsonConvert.SerializeObject(obj);
                return JsonConvert.DeserializeObject(json, targetType);
            }

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