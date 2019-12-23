using System;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Expressions;
using Elsa.Scripting.JavaScript.Converters;
using Elsa.Scripting.JavaScript.Extensions;
using Elsa.Scripting.JavaScript.Messages;
using Elsa.Scripting.JavaScript.Options;
using Elsa.Services.Models;
using Jint;
using Jint.Native;
using MediatR;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Scripting.JavaScript.Services
{
    public class JavaScriptHandler : IWorkflowScriptExpressionHandler
    {
        private readonly IMediator mediator;
        private readonly IMapper mapper;
        private readonly IOptions<ScriptOptions> options;
        private readonly JsonSerializerSettings serializerSettings;

        public JavaScriptHandler(IMediator mediator, IMapper mapper, IOptions<ScriptOptions> options)
        {
            this.mediator = mediator;
            this.mapper = mapper;
            this.options = options;

            serializerSettings = new JsonSerializerSettings().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            serializerSettings.Converters.Add(new TruncatingNumberJsonConverter());
        }

        public string Type => JavaScriptExpression.ExpressionType;

        public async Task<object> EvaluateAsync(IWorkflowExpression expression, ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var javaScriptExpression = (JavaScriptExpression)expression;
            var engine = new Engine(ConfigureJintEngine);

            await ConfigureEngineAsync(engine, context, cancellationToken);
            engine.Execute(javaScriptExpression.Script);

            return ConvertValue(engine.GetCompletionValue(), javaScriptExpression.ReturnType);
        }

        // ReSharper disable once ParameterHidesMember
        private void ConfigureJintEngine(Jint.Options options)
        {
            if (this.options.Value.AllowClr)
                options.AllowClr();
        }

        private async Task ConfigureEngineAsync(Engine engine, ActivityExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            await mediator.Publish(new EvaluatingJavaScriptExpression(engine, workflowExecutionContext), cancellationToken);
        }

        private object ConvertValue(JsValue value, Type targetType)
        {
            if (value.IsUndefined())
                return null;

            if (value.IsNull())
                return default;

            if (targetType == typeof(bool) && value.IsBoolean())
                return value.AsBoolean();

            if (targetType == typeof(DateTime) && value.IsDate())
                return value.AsDate().ToDateTime();

            if (targetType.IsNumeric() && value.IsNumber())
                return Convert.ChangeType(value.AsNumber(), targetType);

            if (targetType == typeof(string))
                return value.ToString();

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
                        bytes[i] = (byte) jsValue.AsNumber();
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

                switch (obj)
                {
                    case ExpandoObject _:
                    {
                        var json = JsonConvert.SerializeObject(obj, serializerSettings);
                        return JsonConvert.DeserializeObject(json, targetType, serializerSettings);
                    }
                    case JValue jValue:
                        return jValue.Value;
                    default:
                        return obj;
                }
            }

            throw new ArgumentException($"Value type {value.Type} is not supported.", nameof(value));
        }
    }
}