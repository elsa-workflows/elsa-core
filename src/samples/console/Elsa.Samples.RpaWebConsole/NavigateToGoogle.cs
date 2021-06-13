using Elsa.Activities.Primitives;
using Elsa.Activities.Rpa.Web;
using Elsa.Builders;
using System.Dynamic;

namespace Elsa.Samples.HelloWorldConsole
{
    /// <summary>
    /// A basic workflow demostrating google navigation
    /// </summary>
    public class NavigateToGoogle : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder.StartWith<OpenBrowser>()
                .SetVariable("DriverId",x=>x.Input)
                .Then<NavigateToUrl>(a=>
                    a.Set(x=> x.Url,"https://google.com")
                    .Set(x=> x.DriverId, context => context.GetVariable<string>("DriverId"))
                    )
                .Then<CloseBrowser>(
                    a=>a.Set(x=>x.DriverId,context=>context.GetVariable<string>("DriverId"))
                    )
                ;
        }
    }
}