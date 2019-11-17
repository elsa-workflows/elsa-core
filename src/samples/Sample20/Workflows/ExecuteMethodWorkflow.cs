using Elsa.Activities.Console.Activities;
using Elsa.Activities.Reflection.Activities;
using Elsa.Expressions;
using Elsa.Scripting.JavaScript;
using Elsa.Scripting.Liquid;
using Elsa.Services;
using Elsa.Services.Models;
using Sample20.Services;

namespace Sample20.Workflows
{
    public class ExecuteMethodWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .StartWith<WriteLine>(x => x.TextExpression = new LiteralExpression("Running Execute Method Workflow."))
                .Then<ExecuteMethod>(x =>
                {
                    x.MethodName = nameof(MyUtility.WriteHello);
                    x.TypeName = typeof(MyUtility).AssemblyQualifiedName;
                })
                .Then<WriteLine>(x => x.TextExpression = new LiteralExpression("Enter a number:"))
                .Then<ReadLine>().WithName("NumberA")
                .Then<WriteLine>(x => x.TextExpression = new LiteralExpression("Enter another number:"))
                .Then<ReadLine>().WithName("NumberB")
                .Then<ExecuteMethod>(x =>
                {
                    x.MethodName = nameof(MyUtility.MultiplyNumber);
                    x.Arguments = new JavaScriptExpression<object[]>("[parseFloat(NumberA.Input), parseFloat(NumberB.Input)]");
                    x.TypeName = typeof(MyUtility).AssemblyQualifiedName;
                }).WithName("Result")
                .Then<WriteLine>(x => x.TextExpression = new LiquidExpression<string>("{{ Activities.NumberA.Input }} * {{ Activities.NumberB.Input }} = {{ Activities.Result.Result }}"))
                .Then<WriteLine>(x => x.TextExpression = new LiteralExpression("Thanks for reflecting!"));
        }
    }
}