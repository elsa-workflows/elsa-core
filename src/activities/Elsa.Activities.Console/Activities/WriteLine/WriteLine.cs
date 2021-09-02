using System.IO;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Console
{
    /// <summary>
    /// Writes a text string to the console.
    /// </summary>
    [Action(
        Category = "Console",
        Description = "Write text to standard out.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class WriteLine : Activity
    {
        public WriteLine() : this(System.Console.Out)
        {
        }

        public WriteLine(TextWriter output)
        {
            _output = output;
        }

        [ActivityInput(Hint = "The text to write.", SupportedSyntaxes = new[]{ SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? Text { get; set; }

        private readonly TextWriter _output;

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            await _output.WriteLineAsync(Text);
            context.JournalData.Add(nameof(Text), Text);
            return Done();
        }
    }
}