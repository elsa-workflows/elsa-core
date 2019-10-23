using System;
using Elsa.Services;
using Elsa.Results;
using Elsa.Services.Models;

namespace Sample01.Activities
{
    public class HelloWorld : Activity
    {
        protected override ActivityExecutionResult OnExecute(WorkflowExecutionContext context)
        {
            Console.WriteLine("Hello world!");
            return Done();
        }
    }
}