using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;

namespace Elsa.Activities.File
{
    [Action(Category = "File",
        Description = "Created a temporary file and returns its path",
        Outcomes = new[] { OutcomeNames.Done })]
    public class TempFile : Activity
    {
        [ActivityOutput(Hint = "Path of the created temporary file.")]
        public string Path { get; set; }

        protected override IActivityExecutionResult OnExecute()
        {
            Path = System.IO.Path.GetTempFileName();
            return Done();
        }
    }
}
