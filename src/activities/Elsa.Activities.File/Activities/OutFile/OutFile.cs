using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.File
{
    [Action(Category = "File",
        Description = "Output input value to specified location",
        Outcomes = new[] { OutcomeNames.Done })]
    public class OutFile : Activity
    {
        [Required]
        [ActivityInput(Hint = "Bytes to write to file.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript }, DefaultSyntax = SyntaxNames.JavaScript)]
        public byte[] Bytes { get; set; } = default!;

        [Required]
        [ActivityInput(Hint = "Path to create file at.", UIHint = ActivityInputUIHints.SingleLine, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string Path { get; set; } = default!;

        [ActivityInput(Hint = "How the output file should be written.", UIHint = ActivityInputUIHints.Dropdown, DefaultValue = CopyMode.CreateNew)]
        public CopyMode Mode { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
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

            var folderPath = System.IO.Path.GetDirectoryName(Path)!;

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
            
            await using (var fs = new FileStream(Path, fileMode, fileAccess))
            {
                await fs.WriteAsync(Bytes);
                await fs.FlushAsync();
            }

            return Done();
        }
    }
}
