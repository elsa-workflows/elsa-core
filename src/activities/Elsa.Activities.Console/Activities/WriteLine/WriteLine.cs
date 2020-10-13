using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Console
{
    /// <summary>
    /// Writes a text string to the console.
    /// </summary>
    [ActivityDefinition(
        Category = "Console",
        Description = "Write text to standard out.",
        Icon = "fas fa-terminal",
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

        [ActivityProperty(Hint = "The text to write.")]
        public string Text { get; set; }

        private readonly TextWriter _output;

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(
            ActivityExecutionContext context,
            CancellationToken cancellationToken)
        {
            await _output.WriteLineAsync(Text);
            return Done();
        }
    }
}