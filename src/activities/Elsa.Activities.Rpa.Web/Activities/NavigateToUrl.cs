using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services.Models;
using System;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Rpa.Web
{
    [Action(Category = "Rpa.Web", Description = "Navigates to a URL")]
    public class NavigateToUrl : WebActivity
    {
        public NavigateToUrl(IServiceProvider sp) : base(sp)
        {
        }

        [ActivityInput(Hint = "The URL to navigate to",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? Url { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            return await ExecuteDriver(context, driver =>
            {
                driver.Navigate().GoToUrl(Url);
                return Task.CompletedTask;
            });
        }
    }
}