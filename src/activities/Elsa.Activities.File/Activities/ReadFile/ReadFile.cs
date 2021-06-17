using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Activities.File
{
    [Action(Category = "File",
        Description = "Output input value to specified location",
        Outcomes = new[] { OutcomeNames.Done })]
    public class ReadFile : Activity
    {
        [ActivityInput(Hint = "Path to read content from.", UIHint = ActivityInputUIHints.SingleLine, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string Path { get; set; }

        [ActivityOutput(Hint = "Bytes of the file read.")]
        public byte[] Bytes { get; set; }

        public async override ValueTask<IActivityExecutionResult> ExecuteAsync(ActivityExecutionContext context)
        {
            var Bytes = await System.IO.File.ReadAllBytesAsync(Path);
            return Done();
        }
    }
}
