using Elsa.Activities.Console;
using Elsa.Builders;

namespace Elsa.Samples.Elasticsearch
{
    /// <summary>
    /// A basic workflow with just one WriteLine activity.
    /// </summary>
    public class HelloWorld : IWorkflow
    {
        public void Build(IWorkflowBuilder builder) => builder
            .WriteLine("Hello World!");
    }
}