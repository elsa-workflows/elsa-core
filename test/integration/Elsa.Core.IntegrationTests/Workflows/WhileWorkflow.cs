﻿using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
using Elsa.Builders;
using Elsa.Services.Models;

namespace Elsa.Core.IntegrationTests.Workflows
{
    public class WhileWorkflow : IWorkflow
    {
        private const string CounterVariableName = "Counter";
        private readonly int _loopCount;

        public WhileWorkflow(int loopCount)
        {
            _loopCount = loopCount;
        }
        
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .Then<While>(
                    @while => @while.WithCondition(context => GetCounter(context) < _loopCount),
                    @while =>
                    {
                        @while
                            .When(While.IterateOutcome)
                            .WriteLine(context => $"Inside while loop. Counter = {context.GetVariable<int>(CounterVariableName)}", id: "WriteLoopCount")
                            .SetVariable(CounterVariableName, context => GetCounter(context) + 1);

                    })
                .Then<WriteLine>(writeLine => writeLine.WithText("Done")).WithName("Done");
        }
        
        private int GetCounter(ActivityExecutionContext context) => context.GetVariable<int>(CounterVariableName);
    }
}