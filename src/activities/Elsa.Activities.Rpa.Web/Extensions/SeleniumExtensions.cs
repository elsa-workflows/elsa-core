using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.Extensions;
using IAlert = OpenQA.Selenium.IAlert;
using IWebElement = OpenQA.Selenium.IWebElement;

namespace Elsa.Activities.Rpa.Web
{
    public static class SeleniumExtensions
    {
        public static HtmlDocument GetPageDocument(this IWebDriver element)
        {
            var doc = new HtmlDocument();
            var html = "";
            if (element != null)
                html = element.PageSource;
            doc.LoadHtml(html);
            return doc;
        }
        public static HtmlDocument GetPageDocument(this ISearchContext element)
        {
            var doc = new HtmlDocument();
            var html = "";
            if (element as OpenQA.Selenium.IWebElement != null)
                html = ((OpenQA.Selenium.IWebElement)element).GetAttribute("outerHtml");
            if (string.IsNullOrWhiteSpace(html))
                html = ((OpenQA.Selenium.IWebElement)element).GetAttribute("outerHTML");
            if (element as IWebDriver != null)
                html = ((OpenQA.Selenium.IWebDriver)element).PageSource;

            doc.LoadHtml(html);
            return doc;
        }

        public static IAlert? TryGetAlert(this ITargetLocator locator)
        {
            try
            {
                return locator.Alert();
            }
            catch
            {
                return null;
            }
        }
        public static bool HasAlert(this IWebDriver driver) => driver.SwitchTo().TryGetAlert() != null;

        public static void DoubleClick(this IWebElement element)
        {
            var remote = (RemoteWebElement)element;
            var action = new Actions(remote.WrappedDriver);
            action.DoubleClick(element).Build().Perform();
        }

        public static void RightClick(this IWebElement element)
        {
            var remote = (RemoteWebElement)element;
            var action = new Actions(remote.WrappedDriver);
            action.ContextClick(element).Build().Perform();
        }

        public static void JavaScriptClick(this IWebElement element)
        {
            var remote = (RemoteWebElement)element;
            remote.WrappedDriver.ExecuteJavaScript("arguments[0].click()", element);
        }

        public static IWebElement GetElementByJQuery(this IWebDriver driver, string cssSelector)
        {
            return driver.ExecuteJavaScript<IWebElement>($"return $(\"{cssSelector}\")[0]");
        }
        public static IWebElement GetNestedElementByJQuery(this IWebElement element, string cssSelector)
        {
            var remote = (RemoteWebElement)element;
            return remote.WrappedDriver.ExecuteJavaScript<IWebElement>($"return $(arguments[0]).find(\"{cssSelector}\")[0]", element);
        }

        public static IWebElement GetFirstAvailableElementByJQuery(this IWebDriver driver, string cssSelector)
        {
            var elements = driver.ExecuteJavaScript<IEnumerable<IWebElement>>($"return $(\"{cssSelector}\")");
            return elements.FirstOrDefault(x => x.Displayed && x.Enabled);
        }
        /// <summary>
        /// "avoid using this method as much as possible. It requires an active Remote Desktop Session to be performed"
        /// </summary>
        public static void NativeClick(this IWebElement element)
        {
            var remote = (RemoteWebElement)element;
            var action = new Actions(remote.WrappedDriver);
            action.Click(element).Build().Perform();
        }

        public static void SetText(this IWebElement element, string text)
        {
            if (text == null)
                text = "";
            text = text.Replace(Environment.NewLine, @"\r\n");
            text = text.Replace("'", @"\'");
            text = JavaScriptEncoder.Default.Encode(text);
            var remote = (RemoteWebElement)element;
            var field = element.IsInputTag() ? "value" : "innerText";
            remote.WrappedDriver.ExecuteJavaScript($"arguments[0].{field} = '{text}'", element);
        }
        public static void JQuerySetText(this IWebElement element, string text)
        {
            text = text.Replace(Environment.NewLine, @"\r\n");
            text = text.Replace("'", @"\'");
            text = JavaScriptEncoder.Default.Encode(text);
            var remote = (RemoteWebElement)element;
            remote.WrappedDriver.ExecuteJavaScript(
               $"$(arguments[0]).val('{text}'); " +
                "$(arguments[0]).trigger('change'); ", element);
        }
        public static void SlideRangeConfirm(this IWebElement element, int valMax)
        {
            var remote = (RemoteWebElement)element;
            remote.WrappedDriver.ExecuteJavaScript(
                "$(arguments[0]).val('" + valMax + "'); " +
                "$(arguments[0]).scope().moving = true; " +
                "$(arguments[0]).scope().clicked = true; " +
                "$(arguments[0]).triggerHandler('change'); " +
                "$(arguments[0]).triggerHandler('mouseup'); ", element);
        }
        public static void SetAttribute(this IWebElement element, string attribute, string value)
        {
            if (value == null)
                value = "";
            value = value.Replace("'", @"\'");
            var remote = (RemoteWebElement)element;
            remote.WrappedDriver.ExecuteJavaScript($"arguments[0].{attribute} = '{value}'", element);
        }

        public static bool IsInputTag(this IWebElement element)
        {
            return string.Equals(element.TagName, "input", StringComparison.OrdinalIgnoreCase) || string.Equals(element.TagName, "textarea", StringComparison.OrdinalIgnoreCase);
        }

        public static TResult? EnsureValue<TResult>(this IWebElement element, Func<IWebElement, TResult> function)
        {
            try
            {
                return function(element);
            }
            catch
            {
                return default;
            }
        }

        public static TResult? EnsureOperation<TResult>(this IWebDriver driver, Func<IWebDriver, TResult> function)
        {
            try
            {
                return function(driver);
            }
            catch
            {
                return default;
            }
        }

        public static void ScrollToEnd(this IWebElement element)
        {
            var remote = (RemoteWebElement)element;
            remote.WrappedDriver.ExecuteJavaScript("arguments[0].scrollLeft = arguments[0].offsetWidth", element);
        }

        public static void Scroll(this IWebElement element, Direction direction, int offset)
        {
            var remote = (RemoteWebElement)element;
            var scroll = direction == Direction.Right || direction == Direction.Down
                ? "+="
                : direction == Direction.Left || direction == Direction.Up
                    ? "-="
                    : throw new ArgumentException("Direction not supported");

            var scrolling = direction == Direction.Right || direction == Direction.Left ? "scrollLeft" : "scrollTop";
            remote.WrappedDriver.ExecuteJavaScript($"arguments[0].{scrolling} {scroll} {offset}", element);
        }

        public static void ScrollWindow(this IWebDriver driver, Direction direction, int offset)
        {
            var horizontalScroll = direction == Direction.Right
              ? offset
              : direction == Direction.Left
                ? offset * -1
                : 0;
            var verticalScroll = direction == Direction.Down
              ? offset
              : direction == Direction.Up
                ? offset * -1
                : 0;
            driver.ExecuteJavaScript($"window.scrollBy({horizontalScroll}, {verticalScroll});");
        }

        public static T GetProperty<T>(this IWebElement element, string name)
        {
            var remote = (RemoteWebElement)element;
            return remote.WrappedDriver.ExecuteJavaScript<T>($"return arguments[0].{name}", element);
        }
        public static async Task<IQueryable<HtmlNode>?> GetHtmlNodes(this ISearchContext element, TimeSpan timeout = default)
        {
            if (timeout == default)
                timeout = TimeSpan.FromSeconds(20);
            var doc = new HtmlDocument();
            var start = DateTime.Now;

            IQueryable<HtmlNode>? nodes = default;
            var html = "";
            while (string.IsNullOrEmpty(html) && (DateTime.Now - start) < timeout)
            {
                await Task.Delay(100);
                if (element is IWebElement webElement)
                {
                    var outerHtml = webElement.GetAttribute("outerHtml") ?? webElement.GetAttribute("outerHTML");
                    if (outerHtml == null) continue;
                    html = outerHtml;
                }
                if (element is IWebDriver webDriver)
                    html = webDriver.PageSource;

                if (html == null) continue;
                doc.LoadHtml(html);
                nodes = doc.DocumentNode.Descendants().AsQueryable();
            }
            return nodes;
        }
        public static async Task<HtmlNode> GetHtmlNode(this ISearchContext element, Func<HtmlNode, bool> query, int index = 0, TimeSpan timeout = default, ILogger? logger = default)
        {
            if (timeout == default)
                timeout = TimeSpan.FromSeconds(20);
            if (index < 0)
                throw new ArgumentOutOfRangeException("Index must be positive or zero");
            var doc = new HtmlDocument();
            var start = DateTime.Now;

            HtmlNode? node = null;
            while (node == null && (DateTime.Now - start) < timeout)
            {
                try
                {
                    logger?.LogTrace($"before getting page source");
                    var html = "";
                    if (element as IWebElement != null)
                    {
                        var outerHtml = ((IWebElement)element).GetAttribute("outerHtml");
                        if (outerHtml == null)
                            outerHtml = ((IWebElement)element).GetAttribute("outerHTML");
                        if (outerHtml == null) continue;
                        html = outerHtml;
                    }
                    if (element as IWebDriver != null)
                        html = ((IWebDriver)element).PageSource;

                    if (html == null) continue;
                    else
                        logger?.LogTrace($"page source loaded: {html}");
                    doc.LoadHtml(html);
                    node = doc.DocumentNode.Descendants()
                        .Where(query)
                        .Skip(index - 1)
                        .FirstOrDefault();
                }
                finally
                {
                    await Task.Delay(1000);
                }
            }
            if (node == null)
                throw new Exception("Element not found. Check searching criterias");
            return node;
        }
        #region FindElement
        public static async Task<IWebElement> FindElement<TChild>(this IWebElement element, Func<HtmlNode, bool> query, int index = 0, TimeSpan timeout = default, ILogger? logger = default)
        {
            var node = await GetHtmlNode(element, query, index, timeout, logger);
            if (node == null)
                throw new Exception("Element not found. Check searching criterias");
            return element.FindElement(By.XPath(GetXPath(node)));
        }
        /// <summary>
        /// HtmlAgilityPack must be referenced in order for this extension method method to be callable
        /// </summary>
        public static async Task<IWebElement> FindElement(this ISearchContext element, Func<HtmlNode, bool> query, int index = 0, TimeSpan timeout = default, ILogger? logger = default)
        {
            var node = await GetHtmlNode(element, query, index, timeout, logger);
            return element.FindElement(By.XPath(GetXPath(node)));
        }
        public static IWebElement FindElement(this ISearchContext element, HtmlNode node)
        {
            return element.FindElement(By.XPath(GetXPath(node)));
        }
        #endregion
        #region FindElements
        public static async Task<IEnumerable<IWebElement>> FindElements(this IWebDriver element, Func<HtmlNode, bool> query, TimeSpan timeout = default)
        {
            var output = new List<IWebElement>();
            var nodes = await GetHtmlNodes(element, timeout);
            if (nodes == null)
                throw new Exception($"Descendants elements not found for element {element}. Check searching criterias");

            var selectedNodes = nodes.Where(query);
            foreach (var selectedNode in selectedNodes)
            {
                output.Add(GetWebElement(selectedNode, element));
            }
            return output;
        }
        public static async Task<IEnumerable<IWebElement>> FindElements<TChild>(this IWebElement element, Func<HtmlNode, bool> query, TimeSpan timeout = default)
        {
            var output = new List<IWebElement>();
            var nodes = await GetHtmlNodes(element, timeout);
            if (nodes == null)
                throw new Exception($"Descendants elements not found for element {element}. Check searching criterias");

            var selectedNodes = nodes.Where(query);
            foreach (var selectedNode in selectedNodes)
            {
                output.Add(GetWebElement(selectedNode, element));
            }
            return output;
        }
        public static string GetXPath(HtmlNode node)
        {
            return ".//" + string.Join("/", node.XPath.Split('/').Skip(2).ToArray());
        }
        public static IWebElement GetWebElement(this HtmlNode node, ISearchContext context)
        {
            return context.FindElement(By.XPath(GetXPath(node)));
        }
        public static IWebElement GetWebElement(this IWebElement context, HtmlNode node)
        {
            return GetWebElement(node, context);
        }
        public static IWebElement GetWebElement(this HtmlNode node, IWebElement context)
        {
            return context.FindElement(By.XPath(GetXPath(node)));
        }
        #endregion
    }
}
