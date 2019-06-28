using System;
using Elsa.Core.Services;
using Elsa.Results;
using Elsa.Services.Models;

namespace Sample01.Activities
{
    public class GoodByeWorld : Activity
    {
        protected override ActivityExecutionResult OnExecute(WorkflowExecutionContext context)
        {
            Console.WriteLine("Goodbye cruel world...");
            return Done();
        }
    }
}