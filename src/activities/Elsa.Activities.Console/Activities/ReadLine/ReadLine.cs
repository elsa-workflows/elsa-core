using System.IO;
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
    [Action(
        Category = "Console",
        Description = "Read text from standard in.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class ReadLine : Activity
    {
        private readonly TextReader? _input;

        [ActivityOutput] public string? Output { get; set; }

        public ReadLine() : this(System.Console.In)
        {
        }

        public ReadLine(TextReader input)
        {
            _input = input;
        }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            if (_input == null)
                return Suspend();

            var receivedInput = await _input.ReadLineAsync();
            return Execute(context, receivedInput);
        }

        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context)
        {
            var receivedInput = (string) context.Input!;
            return Execute(context, receivedInput);
        }

        private IActivityExecutionResult Execute(ActivityExecutionContext context, string receivedInput)
        {
            Output = receivedInput;
            context.JournalData.Add("Input", receivedInput);
            return Done();
        }
    }
}