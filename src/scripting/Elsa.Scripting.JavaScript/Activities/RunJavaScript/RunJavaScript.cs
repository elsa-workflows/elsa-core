using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Metadata;
using Elsa.Scripting.JavaScript.Events;
using Elsa.Scripting.JavaScript.Services;
using Elsa.Services;
using Elsa.Services.Models;
using Jint;
using MediatR;
using NetBox.Extensions;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.JavaScript
{
    [Action(Category = "Scripting", Description = "Run JavaScript code.")]
    public class RunJavaScript : Activity, INotificationHandler<RenderingTypeScriptDefinitions>, IActivityPropertyOptionsProvider
    {
        private readonly IJavaScriptService _javaScriptService;

        public RunJavaScript(IJavaScriptService javaScriptService)
        {
            _javaScriptService = javaScriptService;
        }

        [ActivityInput(Hint = "The JavaScript to run.", UIHint = ActivityInputUIHints.CodeEditor, OptionsProvider = typeof(RunJavaScript))]
        public string? Script { get; set; }

        [ActivityInput(
            Hint = "The possible outcomes that can be set by the script.",
            UIHint = ActivityInputUIHints.MultiText,
            DefaultSyntax = SyntaxNames.Json,
            SupportedSyntaxes = new[] {SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid}
        )]
        public ICollection<string> PossibleOutcomes { get; set; } = new List<string> {OutcomeNames.Done};
        
        [ActivityOutput] public object? Output { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var script = Script;

            if (script is null or "")
                return Done();

            var outcomes = new HashSet<string>();

            void ConfigureEngine(Engine engine)
            {
                void SetOutcome(string value) => outcomes.Add(value);
                void SetOutcomes(IEnumerable<string> values) => outcomes.AddRange(values);

                engine.SetValue("setOutcome", (Action<string>) SetOutcome);
                engine.SetValue("setOutcomes", (Action<IEnumerable<string>>) SetOutcomes);
            }

            var output = await _javaScriptService.EvaluateAsync(script, typeof(object), context, ConfigureEngine, context.CancellationToken);

            if (!outcomes.Any())
                outcomes.Add(OutcomeNames.Done);

            Output = output;
            return Outcomes(outcomes);
        }

        public Task Handle(RenderingTypeScriptDefinitions notification, CancellationToken cancellationToken)
        {
            var context = notification.Context;

            if (context != nameof(RunJavaScript))
                return Task.CompletedTask;

            notification.Output.AppendLine("declare function setOutcome(name: string);");
            notification.Output.AppendLine("declare function setOutcomes(names: Array<string>);");

            return Task.CompletedTask;
        }

        object IActivityPropertyOptionsProvider.GetOptions(PropertyInfo property) =>
            new
            {
                EditorHeight = "Large",
                Context = nameof(RunJavaScript),
                Syntax = JavaScriptExpressionHandler.SyntaxName
            };
    }
}