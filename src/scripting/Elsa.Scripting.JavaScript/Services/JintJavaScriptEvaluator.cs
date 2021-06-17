using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Scripting.JavaScript.Converters.Jint;
using Elsa.Scripting.JavaScript.Messages;
using Elsa.Scripting.JavaScript.Options;
using Elsa.Services.Models;
using Jint;
using MediatR;
using Microsoft.Extensions.Options;

namespace Elsa.Scripting.JavaScript.Services
{
    public class JintJavaScriptEvaluator : IJavaScriptService
    {
        private readonly IMediator _mediator;
        private readonly IConvertsJintEvaluationResult _resultConverter;
        private readonly ScriptOptions _scriptOptions;

        public JintJavaScriptEvaluator(
            IMediator mediator,
            IOptions<ScriptOptions> scriptOptions,
            IConvertsJintEvaluationResult resultConverter)
        {
            if (scriptOptions is null)
                throw new ArgumentNullException(nameof(scriptOptions));

            _mediator = mediator;
            _resultConverter = resultConverter;
            _scriptOptions = scriptOptions.Value;
        }

        public async Task<object?> EvaluateAsync(
            string expression,
            Type returnType,
            ActivityExecutionContext context,
            Action<Engine>? configureEngine = default,
            CancellationToken cancellationToken = default)
        {
            var engine = await GetConfiguredEngine(configureEngine, context, cancellationToken);
            var result = ExecuteExpressionAndGetResult(engine, expression);
            return _resultConverter.ConvertToDesiredType(result, returnType);
        }

        private async Task<Engine> GetConfiguredEngine(Action<Engine>? configureEngine, ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var engine = new Engine(opts =>
            {
                opts.AddObjectConverter<ByteArrayConverter>();
                if (_scriptOptions.AllowClr)
                    opts.AllowClr();
            });

            configureEngine?.Invoke(engine);

            // Listeners invoked by the mediator might further-configure the engine
            await _mediator.Publish(new EvaluatingJavaScriptExpression(engine, context), cancellationToken);

            return engine;
        }

        private static object? ExecuteExpressionAndGetResult(Engine engine, string expression)
        {
            return engine.Evaluate(expression).ToObject();
        }
    }
}