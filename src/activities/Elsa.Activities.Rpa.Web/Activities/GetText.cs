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
using OpenQA.Selenium.Support.Extensions;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Rpa.Web
{
    [Action(Category = "Rpa.Web", Description = "Extracts text from an element")]
    public class GetText : WebActivityWithSelector
    {
        public GetText(IServiceProvider sp) : base(sp)
        {
        }
        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            return await ExecuteDriver(context, async (driver) =>
            {
                var element = await GetElement(driver);
                string? output = default;
                if (element?.IsInputTag()??false)
                {
                    output = element?.GetAttribute("value");
                }
                if (string.IsNullOrEmpty(output))
                    output = element?.Text;
                Result = Done(output);
            });      
        }        
    }
}