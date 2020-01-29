using Elsa.Activities.Console;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

namespace Sample14
{
    public class HelloWorldWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .StartWith<WriteLine>(activity => activity.Text = new LiteralExpression<string>("Hello World!"));
        }
    }
}