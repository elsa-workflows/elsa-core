using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services.Models;
using System;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Rpa.Web
{
    [Action(Category = "Rpa.Web", Description = "Clicks an element in the GUI")]
    public class TypeText : WebActivityWithSelector
    {
        public TypeText(IServiceProvider sp) : base(sp)
        {
        }

        [ActivityInput(Hint = "Indicates whether not to perform an interactive typing but just emulates a injecting text via javascript")]
        public bool? UseJavascript { get; set; }

        [ActivityInput(
            UIHint = ActivityInputUIHints.MultiText,
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string Text { get; set; } = default!;

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            return await ExecuteDriver(context, async (driver) =>
            {
                if (UseJavascript ?? false)
                    (await GetElement(driver))?.SetText(Text);
                else
                    (await GetElement(driver))?.SendKeys(Text);
            });
        }
    }
}