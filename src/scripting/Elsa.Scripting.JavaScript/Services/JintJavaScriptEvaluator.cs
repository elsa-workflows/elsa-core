using Elsa.Mediator.Contracts;
using Elsa.Models;
using Elsa.Scripting.JavaScript.Contracts;
using Elsa.Scripting.JavaScript.Notifications;
using Elsa.Scripting.JavaScript.Options;
using Jint;
using Microsoft.Extensions.Options;

namespace Elsa.Scripting.JavaScript.Services
{
    public class JintJavaScriptEvaluator : IJavaScriptEvaluator
    {
        private readonly IEventPublisher _mediator;
        private readonly JintOptions _jintOptions;

        public JintJavaScriptEvaluator(IEventPublisher mediator, IOptions<JintOptions> scriptOptions)
        {
            _mediator = mediator;
            _jintOptions = scriptOptions.Value;
        }

        public async Task<object?> EvaluateAsync(string expression,
            Type returnType,
            ExpressionExecutionContext context,
            Action<Engine>? configureEngine = default,
            CancellationToken cancellationToken = default)
        {
            var engine = await GetConfiguredEngine(configureEngine, context, cancellationToken);
            var result = ExecuteExpressionAndGetResult(engine, expression);

            return result;
        }

        private async Task<Engine> GetConfiguredEngine(Action<Engine>? configureEngine, ExpressionExecutionContext context, CancellationToken cancellationToken)
        {
            var engine = new Engine(opts =>
            {
                if (_jintOptions.AllowClrAccess)
                    opts.AllowClr();
            });

            configureEngine?.Invoke(engine);

            // Allow listeners invoked by the mediator to configure the engine.
            await _mediator.PublishAsync(new EvaluatingJavaScript(engine, context), cancellationToken);

            return engine;
        }

        private static object? ExecuteExpressionAndGetResult(Engine engine, string expression)
        {
            var result = engine.Execute(expression).GetCompletionValue();
            return result?.ToObject();
        }
    }
}