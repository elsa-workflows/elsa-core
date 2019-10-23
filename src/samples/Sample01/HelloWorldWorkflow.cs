using Elsa.Services;
using Elsa.Services.Models;
using Sample01.Activities;

namespace Sample01
{
    public class HelloWorldWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .StartWith<HelloWorld>()
                .Then<GoodByeWorld>();
        }
    }
}