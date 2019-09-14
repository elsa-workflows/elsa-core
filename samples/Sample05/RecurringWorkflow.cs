using System;
using Elsa.Activities.Console.Activities;
using Elsa.Activities.Timers.Activities;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

namespace Sample05
{
    public class RecurringWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WithId("RecurringWorkflow")
                .AsSingleton()
                .StartWith<TimerEvent>(x => x.TimeoutExpression = new PlainTextExpression<TimeSpan>("00:00:05"), "Timer")
                .Then<WriteLine>(x => x.TextExpression = new JavaScriptExpression<string>("`Trigger received. The time is: ${new Date().toISOString()}`"))
                .Then("Timer");
        }
    }
}