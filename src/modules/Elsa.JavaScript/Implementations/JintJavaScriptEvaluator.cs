using System.Dynamic;
using System.Text.Json;
using Elsa.Expressions.Models;
using Elsa.JavaScript.Extensions;
using Elsa.JavaScript.Notifications;
using Elsa.JavaScript.Options;
using Elsa.JavaScript.Services;
using Elsa.Mediator.Services;
using Elsa.Workflows.Core;
using Jint;
using Microsoft.Extensions.Options;

namespace Elsa.JavaScript.Implementations
{
    /// <summary>
    /// Provides a JavaScript evaluator using Jint.
    /// </summary>
    public class JintJavaScriptEvaluator : IJavaScriptEvaluator
    {
        private readonly IEventPublisher _mediator;
        private readonly JintOptions _jintOptions;

        /// <summary>
        /// Constructor.
        /// </summary>
        public JintJavaScriptEvaluator(IEventPublisher mediator, IOptions<JintOptions> scriptOptions)
        {
            _mediator = mediator;
            _jintOptions = scriptOptions.Value;
        }

        /// <inheritdoc />
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
            
            // Add common functions.
            engine.SetValue("setVariable", (Action<string, object>)((name, value) => context.SetVariable(name, value)));
            
            // ReSharper disable once ConvertClosureToMethodGroup (Jint will not understand).
            engine.SetValue("getVariable", (Func<string, object?>)(name => context.GetVariable(name)));
            
            engine.SetValue("isNullOrWhiteSpace", (Func<string, bool>)string.IsNullOrWhiteSpace);
            engine.SetValue("isNullOrEmpty", (Func<string, bool>)string.IsNullOrEmpty);

            // Add common .NET types.
            engine.RegisterType<DateTime>();
            engine.RegisterType<TimeSpan>();

            // Allow listeners invoked by the mediator to configure the engine.
            await _mediator.PublishAsync(new EvaluatingJavaScript(engine, context), cancellationToken);

            return engine;
        }

        private static object? ExecuteExpressionAndGetResult(Engine engine, string expression)
        {
            var result = engine.Evaluate(expression);
            return result.ToObject();
        }
    }
}