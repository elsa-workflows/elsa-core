using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.File
{
    [Action(Category = "File",
        Description = "Deletes file at specified location",
        Outcomes = new[] { OutcomeNames.Done })]
    public class DeleteFile : Activity
    {
        [ActivityInput(Hint = "Path of the file to delete.")]
        public string Path { get; set; } = default!;

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            System.IO.File.Delete(Path);
            return Done();
        }
    }
}
