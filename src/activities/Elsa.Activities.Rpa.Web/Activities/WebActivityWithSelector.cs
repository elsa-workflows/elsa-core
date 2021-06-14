using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using HtmlAgilityPack;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Rpa.Web
{
    public class WebActivityWithSelector : WebActivity
    {
        public WebActivityWithSelector(IServiceProvider sp) : base(sp)
        {
        }
        [ActivityInput(
            UIHint = ActivityInputUIHints.Dropdown,
            Hint = "The type of selector to be used to identity the element",
            Options = new[] { SelectorTypes.ByName, SelectorTypes.ById, SelectorTypes.ByCss },
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? SelectorType { get; set; }
        [ActivityInput(Hint = "The selector value depends on SelectorType")]
        public string? SelectorValue { get; set; }        
        public Func<HtmlNode, bool>? AdvancedSelector { get; set; }
        internal IWebElement GetElement(IWebDriver driver)
        {
            var els = GetElements(driver);
            if (!els.Any())
                throw new Exception($"no element found matching the given criterias");
#pragma warning disable CS8603 // Possible null reference return.
            return els.FirstOrDefault();
#pragma warning restore CS8603 // Possible null reference return.
        }
        internal IEnumerable<IWebElement?> GetElements(IWebDriver driver)
        {
            if (AdvancedSelector != default)
                return driver.FindElements(AdvancedSelector);
            switch (SelectorType)
            {
                case SelectorTypes.ById: { return driver.FindElements(By.Id(SelectorValue)); }
                case SelectorTypes.ByName: { return driver.FindElements(By.Name(SelectorValue)); }
                case SelectorTypes.ByXPath: { return driver.FindElements(By.XPath(SelectorValue)); }
                case SelectorTypes.ByLinkText: { return driver.FindElements(By.LinkText(SelectorValue)); }
                case SelectorTypes.Advanced:
                    {                        
                        var options = ScriptOptions.Default.AddReferences(typeof(HtmlNode).Assembly);
                        try
                        {
                            Func<HtmlNode, bool> exp = CSharpScript.EvaluateAsync<Func<HtmlNode, bool>>(SelectorValue, options).GetAwaiter().GetResult();
                            return driver.FindElements(exp);
                        }
                        catch (Exception e)
                        {
                            throw new Exception($"invalid expression {SelectorValue}", e);
                        }
                    }
                default: return new List<IWebElement>();
            }
        }
    }
}