using System;
using System.Net.Http;
using Elsa.Activities.Console;
using Elsa.Activities.Http;
using Elsa.Builders;

namespace Elsa.Samples.FaultyWorkflows.Workflows
{
    public class FaultyWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .HttpEndpoint(activity => activity.WithPath("/faulty").WithMethod(HttpMethod.Get.Method))
                .WriteLine("Catch this!")
                .Then(() => throw new ArithmeticException("Does not compute", new ArgumentException("Incorrect argument", new ArgumentOutOfRangeException("This is the root problem", default(Exception)))));
        }
    }
}