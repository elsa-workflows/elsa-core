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
        Description = "Output input value to specified location",
        Outcomes = new[] { OutcomeNames.Done })]
    public class OutFile : Activity
    {
        [ActivityProperty(Hint = "Path to create file at.")]
        public string Path { get; set; }

        [ActivityProperty(Hint = "How the output file should be written to.")]
        public CopyMode Mode { get; set; } = CopyMode.CreateNew;

        protected async override ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            FileStream fs;
            switch (Mode)
            {
                case CopyMode.Append:
                    fs = new FileStream(Path, FileMode.Append, FileAccess.Write);
                    break;
                case CopyMode.Overwrite:
                    fs = new FileStream(Path, FileMode.Create, FileAccess.ReadWrite);
                    break;
                case CopyMode.CreateNew:
                    fs = new FileStream(Path, FileMode.CreateNew, FileAccess.ReadWrite);
                    break;
                default:
                    throw new ApplicationException("Unimplemented copy mode");
            }

            using var sw = new StreamWriter(fs);
            await sw.WriteLineAsync((string)context.Input);
            await sw.FlushAsync();
            await fs.FlushAsync();

            return Done();
        }
    }
}
