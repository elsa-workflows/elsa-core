using Elsa.Activities.Primitives;
using Elsa.Activities.Rpa.Web;
using Elsa.Builders;
using System.Dynamic;
using HtmlAgilityPack;

namespace Elsa.Samples.RpaWebConsole
{
    /// <summary>
    /// A basic workflow demostrating a website navigation
    /// </summary>
    public class NavigateToWebsite : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder.StartWith<OpenBrowser>()
                //.SetVariable("MyDriverId",x=>x.Input)//this is optional, to handle multiple driver instances
                .Then<NavigateToUrl>(a=>
                    a
                    .Set(x=> x.Url,"https://lucapisano.it")
                    //.Set(x=> x.DriverId, context => context.GetVariable<string>("MyDriverId"))//this is optional, to handle multiple driver instances
                    )
                .Then<ClickElement>(a=>
                    //a.Set(x=> x.AdvancedSelector, s=>s.Name=="div" && s.InnerText=="Accetto")
                    a.Set(x=> x.SelectorType, SelectorTypes.Advanced)
                    //.Set(x=> x.SelectorValue, "s=>s.Name==\"div\" && s.InnerText.Contains(\"I agree\")")
                    .Set(x=> x.SelectorValue, "s=>s.Name==\"a\" && s.InnerText.Contains(\"Credly\")")
                    )
                .Then<CloseBrowser>()
                ;
        }
    }
}