using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using System.ComponentModel.DataAnnotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.File
{
    [Action(Category = "File",
        Description = "Deletes file at specified location",
        Outcomes = new[] { OutcomeNames.Done })]
    public class DeleteFile : Activity
    {
        [Required]
        [ActivityInput(Hint = "Path of the file to delete.",  UIHint = ActivityInputUIHints.SingleLine, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? Path { get; set; }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            System.IO.File.Delete(Path);
            return Done();
        }
    }
}
