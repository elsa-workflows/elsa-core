﻿using Elsa.Mediator.Services;
using Elsa.Models;
using Elsa.Scripting.JavaScript.Extensions;
using Elsa.Scripting.JavaScript.Notifications;
using Elsa.Scripting.JavaScript.Options;
using Elsa.Scripting.JavaScript.Services;
using Jint;
using Microsoft.Extensions.Options;

namespace Elsa.Scripting.JavaScript.Implementations
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

        private IDictionary<string, RegisterLocation> GetVariables(ExpressionExecutionContext context)
        {
            var currentContext = context;
            var dictionary = new Dictionary<string, RegisterLocation>();

            while (currentContext != null)
            {
                foreach (var l in currentContext.Register.Locations)
                {
                    if(!dictionary.ContainsKey(l.Key))
                        dictionary.Add(l.Key, l.Value);
                }
                
                currentContext = currentContext.ParentContext;
            }

            return dictionary;
        }
    }
}