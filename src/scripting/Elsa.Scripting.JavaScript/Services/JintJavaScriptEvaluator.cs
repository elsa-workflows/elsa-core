using System;
using System.Threading;
using System.Threading.Tasks;
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
        readonly IMediator mediator;
        readonly IConvertsJintEvaluationResult resultConverter;
        readonly ScriptOptions scriptOptions;

        public JintJavaScriptEvaluator(IMediator mediator,
                                       IOptions<ScriptOptions> scriptOptions,
                                       IConvertsJintEvaluationResult resultConverter)
        {
            if (scriptOptions is null)
                throw new ArgumentNullException(nameof(scriptOptions));

            this.mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            this.resultConverter = resultConverter ?? throw new ArgumentNullException(nameof(resultConverter));
            this.scriptOptions = scriptOptions.Value;
        }
        
        public async Task<object?> EvaluateAsync(string expression,
                                                 Type returnType,
                                                 ActivityExecutionContext context,
                                                 Action<Engine>? configureEngine = default,
                                                 CancellationToken cancellationToken = default)
        {
            var engine = await GetConfiguredEngine(configureEngine, context, cancellationToken);
            var result = ExecuteExpressionAndGetResult(engine, expression);
            return resultConverter.ConvertToDesiredType(result, returnType);
        }

        async Task<Engine> GetConfiguredEngine(Action<Engine>? configureEngine, ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var engine = new Engine(opts => {
                if (scriptOptions.AllowClr)
                    opts.AllowClr();
            });

            configureEngine?.Invoke(engine);

            // Listeners invoked by the mediator might further-configure the engine
            await mediator.Publish(new EvaluatingJavaScriptExpression(engine, context), cancellationToken);

            return engine;
        }

        object? ExecuteExpressionAndGetResult(Engine engine, string expression)
        {
            engine.Execute(expression);
            return engine.GetCompletionValue().ToObject();
        }
    }
}