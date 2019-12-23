using System;
using Elsa.Services;
using Elsa.Services.Models;

namespace Sample01.Activities
{
    public class HelloWorld : Activity
    {
        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            Console.WriteLine("Hello world!");
            return Done();
        }
    }
}