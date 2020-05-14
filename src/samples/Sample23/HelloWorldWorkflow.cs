using Elsa.Activities.Console.Activities;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

namespace Sample23
{
    public class HelloWorldWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WithVersion(1)
                .StartWith<WriteLine>(activity => activity.TextExpression = new LiteralExpression("Hello World!"));
        }
    }
}