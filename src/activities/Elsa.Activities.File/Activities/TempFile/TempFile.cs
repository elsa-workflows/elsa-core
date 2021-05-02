using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Elsa.Activities.File
{
    [Action(Category = "File",
        Description = "Created a temporary file and returns its path",
        Outcomes = new[] { OutcomeNames.Done })]
    public class TempFile : Activity
    {
        protected override IActivityExecutionResult OnExecute()
        {
            return Done(Path.GetTempFileName());
        }
    }
}
