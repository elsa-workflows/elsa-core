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
    /// Reads input from the console.
    /// </summary>
    [ActivityDefinition(
        Category = "Console",
        Description = "Read text from standard in.",
        Icon = "fas fa-terminal",
        RuntimeDescription =
            "a => !!a.state.variableName ? `Read text from standard in and store into <strong>${ a.state.variableName }</strong>.` : 'Read text from standard in.'",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class ReadLine : Activity
    {
        private readonly TextReader _input;

        public ReadLine()
        {
        }

        public ReadLine(TextReader input)
        {
            _input = input;
        }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(
            ActivityExecutionContext context,
            CancellationToken cancellationToken)
        {
            if (_input == null)
                return Suspend();

            var receivedInput = await _input.ReadLineAsync();
            return Execute(receivedInput);
        }

        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context)
        {
            var receivedInput = (string)context.Input;
            return Execute(receivedInput);
        }

        private IActivityExecutionResult Execute(string receivedInput) => Done(receivedInput);
    }
}