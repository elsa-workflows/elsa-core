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
                .StartWith<CronEvent>(x => x.CronExpression = new PlainTextExpression("* * * * *"))
                .Then<WriteLine>(x => x.TextExpression = new JavaScriptExpression<string>("`Trigger received. The time is: ${new Date().toISOString()}`"));
        }
    }
}