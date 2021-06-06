using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
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
        [ActivityInput(Hint = "Content of the file.", UIHint = ActivityInputUIHints.MultiLine, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string Content { get; set; }

        [ActivityInput(Hint = "Path to create file at.", UIHint = ActivityInputUIHints.SingleLine, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string Path { get; set; }

        [ActivityInput(Hint = "How the output file should be written to.", UIHint = ActivityInputUIHints.Dropdown)]
        public CopyMode Mode { get; set; } = CopyMode.CreateNew;

        protected async override ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            FileMode fileMode;
            FileAccess fileAccess;
            switch (Mode)
            {
                case CopyMode.Append:
                    fileMode = FileMode.Append;
                    fileAccess = FileAccess.Write;
                    break;
                case CopyMode.Overwrite:
                    fileMode = FileMode.Create;
                    fileAccess = FileAccess.ReadWrite;
                    break;
                case CopyMode.CreateNew:
                    fileMode = FileMode.CreateNew;
                    fileAccess = FileAccess.ReadWrite;
                    break;
                default:
                    throw new ApplicationException("Unsupported copy mode");
            }

            using (var fs = new FileStream(Path, fileMode, fileAccess))
            using (var sw = new StreamWriter(fs))
            {
                await sw.WriteLineAsync(Content);
                await sw.FlushAsync();
                await fs.FlushAsync();
            }

            return Done();
        }
    }
}
