using System;
using System.Linq;
using System.Threading.Tasks;
using Elsa;
using Elsa.Activities.Console.Activities;
using Elsa.Activities.Console.Extensions;
using Elsa.Activities.Primitives.Activities;
using Elsa.Activities.Primitives.Extensions;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Models;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleConsoleProgram
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Setup a service collection.
            var services = CreateServiceProvider();

            // Create a workflow invoker.
            var invoker = services.GetService<IWorkflowInvoker>();

            // Create a workflow.
            var workflow = CreateSampleWorkflow();

            // Invoke the workflow.
            await invoker.InvokeAsync(workflow, workflow.Activities.First());

            Console.ReadLine();
        }

        private static Workflow CreateSampleWorkflow()
        {
            // 1. Ask two numbers
            // 2. Ask operation to apply to the two numbers.
            // 3. Show the result of the calculation.
            // 4. Ask user to try again or exit program.

            return new WorkflowBuilder()
                .Add(new WriteLine("Please enter a number:") { Alias = "Start" })
                .Next(new ReadLine("a"))
                .Next(new WriteLine("Please enter another number:"))
                .Next(new ReadLine("b"))
                .Next(new WriteLine("Please enter the operation you would like to perform on the two numbers. Allowed operations:\r\n-Add\r\n-Subtract\r\n-Divide\r\n-Multiply"))
                .Next(new ReadLine("op"))
                .Next(new Switch(JavaScriptEvaluator.SyntaxName, "op"), @switch =>
                {
                    @switch.Next(new SetVariable("result", JavaScriptEvaluator.SyntaxName, "parseFloat(a) + parseFloat(b)"), "Add").Next("ShowResult");
                    @switch.Next(new SetVariable("result", JavaScriptEvaluator.SyntaxName, "a - b"), "Subtract").Next("ShowResult");
                    @switch.Next(new SetVariable("result", JavaScriptEvaluator.SyntaxName, "a * b"), "Multiply").Next("ShowResult");
                    @switch.Next(new SetVariable("result", JavaScriptEvaluator.SyntaxName, "a / b"), "Divide").Next("ShowResult");
                })
                .Next(new WriteLine(new WorkflowExpression<string>(JavaScriptEvaluator.SyntaxName, "`Result: ${result}`")){ Alias = "ShowResult"})
                .Next(new WriteLine("Try again? (Y/N)"))
                .Next(new ReadLine("retry"))
                .Next(new IfElse(new WorkflowExpression<bool>(JavaScriptEvaluator.SyntaxName, "retry.toLowerCase() === 'y'")), ifElse =>
                {
                    ifElse.Next("Start", "True");
                    ifElse.Next(new WriteLine("Bye!"), "False");
                })
                .BuildWorkflow();
        }

        private static IServiceProvider CreateServiceProvider()
        {
            return new ServiceCollection()
                .AddWorkflowsInvoker()
                .AddPrimitiveActivities()
                .AddConsoleActivities()
                .AddSingleton(sp => Console.In)
                .BuildServiceProvider();
        }
    }
}