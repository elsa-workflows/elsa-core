using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Activities.Console.Activities;
using Elsa.Activities.Containers;
using Elsa.Activities.Flowcharts;
using Elsa.Activities.Primitives;
using Elsa.Builders;
using Elsa.Services.Models;

namespace Sample07
{
    public class MyFlowchart : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            var flowchartConfigurator = builder.BuildActivity<FlowchartConfigurator, Flowchart>();

            flowchartConfigurator.StartWith(new Inline(() => Console.WriteLine("Hello World")));
            var flowChart = flowchartConfigurator.Build();
            
            builder
                .StartWith(flowChart);
        }
    }
}