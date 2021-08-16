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
using System.Threading.Tasks;

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
        public int RetryCount { get; set; } = 5;
        public TimeSpan RetryInterval { get; set; } = TimeSpan.FromMilliseconds(500);
        public Func<HtmlNode, bool>? AdvancedSelector { get; set; }
        internal async Task<IWebElement?> GetElement(IWebDriver driver)
        {
            var els = await GetElements(driver);
            if (!els.Any())
                throw new Exception($"No element found matching the given criterias");
            return els.FirstOrDefault();
        }
        internal async Task<IEnumerable<IWebElement?>> GetElements(IWebDriver driver)
        {
            var output = new List<IWebElement>();
            for (int i = 0; i < RetryCount; i++)
            {
                try
                {
                    if (AdvancedSelector != default)
                        return await driver.FindElements(AdvancedSelector);
                    switch (SelectorType)
                    {
                        case SelectorTypes.ById: { output.AddRange(driver.FindElements(By.Id(SelectorValue))); break; }
                        case SelectorTypes.ByName: { output.AddRange(driver.FindElements(By.Name(SelectorValue))); break; }
                        case SelectorTypes.ByXPath: { output.AddRange(driver.FindElements(By.XPath(SelectorValue))); break; }
                        case SelectorTypes.ByLinkText: { output.AddRange(driver.FindElements(By.LinkText(SelectorValue))); break; }
                        case SelectorTypes.Advanced:
                            {
                                var options = ScriptOptions.Default.AddReferences(typeof(HtmlNode).Assembly);
                                try
                                {
                                    Func<HtmlNode, bool> exp = CSharpScript.EvaluateAsync<Func<HtmlNode, bool>>(SelectorValue, options).GetAwaiter().GetResult();
                                    output.AddRange(await driver.FindElements(exp)); break;
                                }
                                catch (Exception e)
                                {
                                    throw new Exception($"Invalid expression {SelectorValue}", e);
                                }
                            }
                        default: return new List<IWebElement>();
                    }
                    if (output.Any())
                        break;
                    else
                        await Task.Delay(RetryInterval);
                }
                catch
                {
                    await Task.Delay(RetryInterval);
                }
            }
            return output;
        }
    }
}