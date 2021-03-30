using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Scripting.JavaScript.Converters;
using Elsa.Scripting.JavaScript.Messages;
using Elsa.Scripting.JavaScript.Options;
using Elsa.Services.Models;
using Jint;
using MediatR;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Scripting.JavaScript.Services
{
    public class JavaScriptService : IJavaScriptService
    {
        private readonly IMediator _mediator;
        private readonly ScriptOptions _options;

        public JavaScriptService(IMediator mediator, IOptions<ScriptOptions> options)
        {
            _mediator = mediator;
            _options = options.Value;

            var serializerSettings = new JsonSerializerSettings().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            serializerSettings.Converters.Add(new TruncatingNumberJsonConverter());
        }
        
        public async Task<object?> EvaluateAsync(string expression, Type returnType, ActivityExecutionContext context, Action<Engine>? configureEngine = default, CancellationToken cancellationToken = default)
        {
            var engine = new Engine(ConfigureJintEngine);

            configureEngine?.Invoke(engine);
            await _mediator.Publish(new EvaluatingJavaScriptExpression(engine, context), cancellationToken);
            engine.Execute(expression);

            var returnValue = engine.GetCompletionValue().ToObject();

            if (returnValue == null)
                return null;
            
            var converter = TypeDescriptor.GetConverter(returnValue);

            if (converter.CanConvertTo(returnType))
                converter.ConvertTo(returnValue, returnType);

            if(returnValue is ExpandoObject expando && returnType == typeof(object))
                return RecursivelyPrepareExpandoObjectForReturn(expando);

            if (returnValue is IEnumerable && !(returnValue is string))
            {
                returnType = (returnType == typeof(object))? typeof(object[]) : returnType;
                var json = JsonConvert.SerializeObject(returnValue);
                return JsonConvert.DeserializeObject(json, returnType);
            }

            if (returnType == typeof(object))
                return returnValue;
            
            return Convert.ChangeType(returnValue, returnType);
        }

        static object? RecursivelyPrepareExpandoObjectForReturn(ExpandoObject obj)
        {
            IDictionary<string,object?> ExpandoToDictionary(ExpandoObject expando)
            {
                var json = JsonConvert.SerializeObject(expando);
                return (IDictionary<string,object?>) JsonConvert.DeserializeObject(json, typeof(Dictionary<string,object?>))!;
            }

            object? EnumerableToObject(IEnumerable enumerable)
            {
                var json = JsonConvert.SerializeObject(enumerable);
                return JsonConvert.DeserializeObject(json, typeof(object[]));
            }

            var expandoDictionary = obj as IDictionary<string,object>;

            var allValuesToReplace = (from kvp in expandoDictionary
                                      where kvp.Value is IEnumerable && !(kvp.Value is string)
                                      let val = (IEnumerable) kvp.Value
                                      let replacementValue = (val is ExpandoObject expando)
                                            ? RecursivelyPrepareExpandoObjectForReturn(expando)
                                            : EnumerableToObject(val)
                                      select new { Key = kvp.Key, Value = replacementValue })
                .ToDictionary(k => k.Key, v => v.Value);

            var output = ExpandoToDictionary(obj);
            foreach(var kvp in allValuesToReplace)
                output[kvp.Key] = kvp.Value;

            return output;
        }
        
        private void ConfigureJintEngine(Jint.Options options)
        {
            if (_options.AllowClr)
                options.AllowClr();
        }
    }
}