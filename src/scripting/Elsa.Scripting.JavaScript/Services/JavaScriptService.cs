using System;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
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
        private readonly JsonSerializerSettings _serializerSettings;

        public JavaScriptService(IMediator mediator, IOptions<ScriptOptions> options)
        {
            _mediator = mediator;
            _options = options.Value;

            _serializerSettings = new JsonSerializerSettings().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            _serializerSettings.Converters.Add(new TruncatingNumberJsonConverter());
        }
        
        public async Task<object?> EvaluateAsync(string expression, Type returnType, ActivityExecutionContext context, Action<Engine>? configureEngine = default, CancellationToken cancellationToken = default)
        {
            var engine = new Engine(ConfigureJintEngine);

            configureEngine?.Invoke(engine);
            await _mediator.Publish(new EvaluatingJavaScriptExpression(engine, context), cancellationToken);
            engine.Execute(expression);

            var returnValue = engine.GetCompletionValue().ToObject();
            var converter = TypeDescriptor.GetConverter(returnValue);

            if (converter.CanConvertTo(returnType))
                converter.ConvertTo(returnValue, returnType);

            if (returnValue is IEnumerable)
            {
                var json = JsonConvert.SerializeObject(returnValue);
                return JsonConvert.DeserializeObject(json, returnType);
            }
            
            return Convert.ChangeType(returnValue, returnType);
        }
        
        private void ConfigureJintEngine(Jint.Options options)
        {
            if (_options.AllowClr)
                options.AllowClr();
        }
    }
}