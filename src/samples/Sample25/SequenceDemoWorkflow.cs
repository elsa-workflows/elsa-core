using System;
using Elsa;
using Elsa.Activities;
using Elsa.Activities.Console.Activities;
using Elsa.Activities.ControlFlow.Activities;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

namespace Sample25
{
    public class SequenceDemoWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .StartWith(() => Console.WriteLine("Welcome to the Sequence activity demo!"))
                .Then<Sequence>(x => x.Activities = new Activity[]
                {
                    new WriteLine { Text = new LiteralExpression<string>("Activity 1") },
                    new WriteLine { Text = new LiteralExpression<string>("Activity 2") },
                    new Sequence
                    {
                        Activities = new Activity[]
                        {
                            new WriteLine { Text = new LiteralExpression<string>("\tChild Activity 1")},
                            new WriteLine { Text = new LiteralExpression<string>("\tChild Activity 2")},
                            new WriteLine { Text = new LiteralExpression<string>("\tChild Activity 3")},
                        }
                    }, 
                    new WriteLine { Text = new LiteralExpression<string>("Activity 3") },
                })
                .Then<WriteLine>(x => x.Text = new LiteralExpression<string>("End of sequence."));
        }
    }
}