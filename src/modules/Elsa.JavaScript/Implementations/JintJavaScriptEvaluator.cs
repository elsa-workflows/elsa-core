using Elsa.Expressions.Models;
using Elsa.JavaScript.Extensions;
using Elsa.JavaScript.Notifications;
using Elsa.JavaScript.Options;
using Elsa.JavaScript.Services;
using Elsa.Mediator.Services;
using Jint;
using Microsoft.Extensions.Options;

namespace Elsa.JavaScript.Implementations
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
            
            // Add workflow variables.
            var variables = GetVariables(context);
            
            foreach (var variable in variables) 
                engine.SetValue(variable.Key, variable.Value.Value);

            // Add common .NET types.
            engine.RegisterType<DateTime>();
            engine.RegisterType<TimeSpan>();

            // Allow listeners invoked by the mediator to configure the engine.
            await _mediator.PublishAsync(new EvaluatingJavaScript(engine, context), cancellationToken);

            return engine;
        }

        private static object? ExecuteExpressionAndGetResult(Engine engine, string expression)
        {
            var result = engine.Execute(expression).GetCompletionValue();
            return result?.ToObject();
        }

        private IDictionary<string, MemoryBlock> GetVariables(ExpressionExecutionContext context)
        {
            var currentRegister = context.Memory;
            var memoryBlocks = new Dictionary<string, MemoryBlock>();

            while (currentRegister != null)
            {
                foreach (var l in currentRegister.Blocks)
                {
                    if(!memoryBlocks.ContainsKey(l.Key))
                        memoryBlocks.Add(l.Key, l.Value);
                }
                
                currentRegister = currentRegister.Parent;
            }

            return memoryBlocks;
        }
    }
}