using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;
using System.ComponentModel.DataAnnotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.File
{
    [Action(Category = "File",
        Description = "Checks if a file exists",
        Outcomes = new[] { OutcomeNames.True, OutcomeNames.False })]
    public class FileExists : Activity
    {
        [Required]
        [ActivityInput(Hint = "Path of the file to check.")]
        public string? Path { get; set; }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            var exists = System.IO.File.Exists(Path);
            return Outcome(exists ? OutcomeNames.True : OutcomeNames.False);
        }
    }
}
