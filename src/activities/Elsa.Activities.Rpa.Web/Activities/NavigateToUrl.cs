using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services.Models;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
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

        [ActivityInput(Hint = "The URL to navigate to")]
        public string? Url { get; set; }
        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            return await ExecuteDriver(context, async(driver) =>
            {
                driver.Navigate().GoToUrl(Url);
            });          
        }
    }
}