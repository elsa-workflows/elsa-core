using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.File
{
    [Action(Category = "File",
        Description = "Checks if a file exists",
        Outcomes = new[] { OutcomeNames.True, OutcomeNames.False })]
    public class FileExists : Activity
    {
        [ActivityInput(Hint = "Path of the file to delete.")]
        public string Path { get; set; } = default!;

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            var exists = System.IO.File.Exists(Path);
            return Outcome(exists ? OutcomeNames.True : OutcomeNames.False);
        }
    }
}
