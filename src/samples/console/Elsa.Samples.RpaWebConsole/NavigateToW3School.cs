using Elsa.Activities.Primitives;
using Elsa.Activities.Rpa.Web;
using Elsa.Builders;
using System.Dynamic;
using HtmlAgilityPack;
using Elsa.Activities.Console;

namespace Elsa.Samples.RpaWebConsole
{
    /// <summary>
    /// A basic workflow demostrating website navigation with input field
    /// </summary>
    public class NavigateToW3School : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder.StartWith<OpenBrowser>(x=>
                x.Set(v=>v.UseHeadless, false)
            )
                .Then<NavigateToUrl>(a =>
                    a                    
                    .Set(x => x.Url, "https://www.w3schools.com/html/html_form_input_types.asp")
                    )
                .Then<ClickElement>(a =>
                    a.Set(x => x.SelectorType, SelectorTypes.ById)
                    .Set(x => x.SelectorValue, "accept-choices")
                    )
                .Then<TypeText>(a =>
                    a.Set(x => x.SelectorType, SelectorTypes.Advanced)
                    .Set(x => x.SelectorValue, "s=>s.Name==\"input\" && s.GetAttributeValue(\"type\",\"\")==\"text\"")
                    .Set(x => x.Text, "test text")
                    )
                .Then<GetText>(a =>
                    a.Set(x => x.SelectorType, SelectorTypes.Advanced)
                    .Set(x => x.SelectorValue, "s=>s.Name==\"input\" && s.GetAttributeValue(\"type\",\"\")==\"text\"")
                    )
                .WriteLine(context => $"Value was '{context.Input}'")
                .Then<CloseBrowser>()
                ;
        }
    }
}