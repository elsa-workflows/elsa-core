using System;
using Elsa.Activities;
using Elsa.Activities.Console.Activities;
using Elsa.Activities.Email.Activities;
using Elsa.Activities.Timers.Activities;
using Elsa.Expressions;
using Elsa.Scripting.Liquid;
using Elsa.Services;
using Elsa.Services.Models;

namespace Sample18
{
    public class EmailReminderWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WithId("RecurringWorkflow")
                .AsSingleton()
                .StartWith<TimerEvent>(x => x.TimeoutExpression = new LiteralExpression<TimeSpan>("00:00:05"))
                .Then<WriteLine>(x => x.TextExpression = new LiteralExpression("Sending reminder"))
                .Then<SetVariable>(x =>
                {
                    x.VariableName = "Todo";
                    x.ValueExpression = new LiteralExpression("Clean up your room.");
                })
                .Then<SendEmail>(x =>
                {
                    x.To = new LiteralExpression<string>("john@acme.com");
                    x.Subject = new LiteralExpression<string>("A kind reminder");
                    x.Body = new LiquidExpression<string>("Here is a kind reminder of the thing you need to do, which is: {{ workflow.Variables.Todo }}");
                });
        }
    }
}