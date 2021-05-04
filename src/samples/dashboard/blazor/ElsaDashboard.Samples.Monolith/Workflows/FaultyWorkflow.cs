using System;
using Elsa.Activities.Console;
using Elsa.Activities.Http;
using Elsa.Builders;
using Elsa.Services.Models;

namespace ElsaDashboard.Samples.Monolith.Workflows
{
    public class FaultyWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WithDisplayName("Faulty Workflow")
                .HttpEndpoint("/faulty")
                .Then(ThrowErrorIfFirstRun)
                .WriteLine("Made it!");
        }

        private static void ThrowErrorIfFirstRun(ActivityExecutionContext context)
        {
            var hasExecuted = context.GetVariable<bool>("HasExecuted");

            if (!hasExecuted)
            {
                context.SetVariable("HasExecuted", true);
                throw new Exception("Something bad happened. Please retry workflow.");
            }
        }
    }
}