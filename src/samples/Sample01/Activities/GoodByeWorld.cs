using System;
using Elsa.Services;
using Elsa.Services.Models;

namespace Sample01.Activities
{
    public class GoodByeWorld : Activity
    {
        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            Console.WriteLine("Goodbye cruel world...");
            return Done();
        }
    }
}