using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Activities.File
{
    [Action(Category = "File",
        Description = "Deletes file at specified location",
        Outcomes = new[] { OutcomeNames.Done })]
    public class DeleteFile : Activity
    {
        [ActivityInput(Hint = "Path of the file to delete.")]
        public string Path { get; set; }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            System.IO.File.Delete(Path);
            return Done();
        }
    }
}
