using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Activities.File
{
    [Action(Category = "File",
        Description = "Output input value to specified location")]
    public class OutFile : Activity
    {
        [ActivityProperty(Hint = "Path to create file at.")]
        public string Path { get; set; }

        protected async override ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            await System.IO.File.WriteAllTextAsync(Path, (string)context.Input);

            return Done();
        }
    }
}
