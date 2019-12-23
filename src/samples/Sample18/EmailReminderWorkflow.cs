using System;
using Elsa;
using Elsa.Activities;
using Elsa.Activities.Console.Activities;
using Elsa.Activities.ControlFlow.Activities;
using Elsa.Activities.Email.Activities;
using Elsa.Activities.Timers.Activities;
using Elsa.Expressions;
using Elsa.Scripting.JavaScript;
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
                .StartWith<WriteLine>(x => x.Text = new LiteralExpression<string>("Enter a reminder"))
                .Then<ReadLine>().WithName("ReminderInput")
                .Then<SetVariable>(
                    x =>
                    {
                        x.VariableName = "Todo";
                        x.ValueScriptExpression = new JavaScriptExpression<string>("ReminderInput.Input");
                    })
                .Then<While>(
                    x => x.Condition = new JavaScriptExpression<bool>("true"),
                    doWhile =>
                    {
                        doWhile
                            .When(OutcomeNames.Iterate)
                            .Then<TimerEvent>(x => x.TimeoutScriptExpression = new LiteralExpression<TimeSpan>("00:00:01"))
                            .Then<WriteLine>(x => x.Text = new LiquidExpression<string>("Sending reminder: \"{{ Activities.ReminderInput.Input }}\""))
                            .Then<SendEmail>(
                                x =>
                                {
                                    x.To = new LiteralExpression<string>("john@acme.com");
                                    x.Subject = new LiteralExpression<string>("A kind reminder");
                                    x.Body = new LiquidExpression<string>("Here is a kind reminder of the thing you need to do, which is: <strong>{{ Variables.Todo }}</strong>");
                                })
                            .Then(doWhile);
                    });
        }
    }
}