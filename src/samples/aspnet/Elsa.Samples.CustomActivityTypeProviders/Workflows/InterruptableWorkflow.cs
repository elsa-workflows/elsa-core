﻿using Elsa.Activities.Console;
using Elsa.Builders;
using Elsa.Samples.CustomActivityTypeProviders.Activities;
using NodaTime;

namespace Elsa.Samples.CustomActivityTypeProviders.Workflows
{
    public class InterruptableWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .WriteLine("This workflow will sleep for 5 minutes before it continues.")
                .WriteLine("Can't wait that long? Send me a message at https://localhost:6171/wakeup.")
                .Then<Sleep>(sleep => sleep.Set(x => x.Timeout, Duration.FromMinutes(5)))
                .WriteLine("Done.");
        }
    }
}