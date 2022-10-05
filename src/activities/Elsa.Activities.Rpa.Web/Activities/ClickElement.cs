using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services.Models;
using System;
using System.Threading.Tasks;
using OpenQA.Selenium.Support.Extensions;
using Elsa.Expressions;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Rpa.Web
{
    [Action(Category = "Rpa.Web", Description = "Clicks an element in the GUI")]
    public class ClickElement : WebActivityWithSelector
    {
        public ClickElement(IServiceProvider sp) : base(sp)
        {
        }

        
        [ActivityInput(Hint = "Indicates whether not to perform an interactive click but just emulates a click via javascript call",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public bool? UseJavascriptClick { get; set; }
        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            return await ExecuteDriver(context, async(driver) =>
            {
                if (UseJavascriptClick ?? false)
                    driver.ExecuteJavaScript("arguments[0].click()", await GetElement(driver));
                else
                    (await GetElement(driver))?.Click();
            });       
        }        
    }
}