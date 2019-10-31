using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Scripting.JavaScript.Messages;
using Elsa.Services;
using Elsa.Services.Models;
using Jint;
using Jint.Native;
using MediatR;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Scripting.JavaScript.Services
{
    public class JavaScriptExpressionEvaluator : IExpressionEvaluator
    {
        private readonly IMediator mediator;
        private readonly JsonSerializerSettings serializerSettings;
        public const string SyntaxName = "JavaScript";

        public static WorkflowExpression<T> CreateExpression<T>(string expression)
        {
            return new WorkflowExpression<T>(SyntaxName, expression);
        }

        public JavaScriptExpressionEvaluator(IMediator mediator)
        {
            this.mediator = mediator;
            serializerSettings = new JsonSerializerSettings().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        }

        public string Syntax => SyntaxName;

        public async Task<object> EvaluateAsync(string expression, Type type, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            var engine = new Engine(options => { options.AllowClr(); });

            await ConfigureEngineAsync(engine, workflowExecutionContext, cancellationToken);
            engine.Execute(expression);

            return ConvertValue(engine.GetCompletionValue(), type);
        }

        private async Task ConfigureEngineAsync(Engine engine, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            await mediator.Publish(new EvaluatingJavaScriptExpression(engine, workflowExecutionContext), cancellationToken);
        }

        private object ConvertValue(JsValue value, Type targetType)
        {
            if (value.IsUndefined())
                return null;

            if (value.IsNull())
                return default;

            if (value.IsBoolean())
                return value.AsBoolean();

            if (value.IsDate())
                return value.AsDate().ToDateTime();

            if (value.IsNumber())
                return value.AsNumber();

            if (value.IsString())
            {
                var stringValue = value.AsString();
                return targetType != null
                    ? targetType == typeof(Uri)
                        ? new Uri(stringValue, UriKind.RelativeOrAbsolute)
                        : Convert.ChangeType(stringValue, targetType)
                    : value.AsString();
            }

            if (value.IsArray())
            {
                var arrayInstance = value.AsArray();
                var elementType = targetType.GetElementType() ?? targetType.GenericTypeArguments.First();

                if (elementType == typeof(byte))
                {
                    var bytes = new byte[arrayInstance.Length];
                    
                    for (uint i = 0; i < arrayInstance.Length; i++)
                    {
                        var jsValue = arrayInstance[i];
                        bytes[i] = (byte)jsValue.AsNumber();
                    }

                    return bytes;
                }
                
                var array = Array.CreateInstance(elementType, arrayInstance.Length);

                for (uint i = 0; i < array.Length; i++)
                {
                    var jsValue = arrayInstance[i];
                    var convertedValue = ConvertValue(jsValue, elementType);
                    array.SetValue(convertedValue, i);
                }

                return array;
            }

            if (value.IsObject())
            {
                var obj = value.AsObject().ToObject();
                var type = targetType ?? obj.GetType();
                var json = JsonConvert.SerializeObject(obj, serializerSettings);
                return JsonConvert.DeserializeObject(json, type, serializerSettings);
            }

            throw new ArgumentException($"Value type {value.Type} is not supported.", nameof(value));
        }
    }
}