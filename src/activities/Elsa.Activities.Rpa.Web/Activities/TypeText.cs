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
            SupportedSyntaxes = new[] { SyntaxNames.Literal }
        )]
        public string Text { get; set; }
        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            try
            {
                var driver = _factory.GetDriver(GetDriverId(context));
                if (UseJavascript ?? false)
                    GetElement(driver).SetText(Text);
                else
                    GetElement(driver).SendKeys(Text);
                return Done();
            }
            catch (Exception e)
            {
                if (GetDriverId(context) != default)
                {
                    _factory.CloseBrowserAsync(GetDriverId(context)).Wait();
                }
                return Fault(e);
            }            
        }        
    }
}