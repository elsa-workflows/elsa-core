using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Activities.File
{
    [Action(Category = "File",
        Description = "Checks if a file exists",
        Outcomes = new[] { OutcomeNames.True, OutcomeNames.False })]
    public class FileExists : Activity
    {
        [ActivityInput(Hint = "Path of the file to delete.")]
        public string Path { get; set; }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            var exists = System.IO.File.Exists(Path);
            if (exists)
                return Outcome(OutcomeNames.True);
            else
                return Outcome(OutcomeNames.False);
        }
    }
}
