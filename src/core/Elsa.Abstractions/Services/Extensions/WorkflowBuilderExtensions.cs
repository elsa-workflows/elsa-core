using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services.Extensions
{
    public static class WorkflowBuilderExtensions
    {
        public static WorkflowDefinitionVersion Build<T>(this IWorkflowBuilder builder) where T : IWorkflow, new()
        {
            var workflow = new T();
            builder.WithId(typeof(T).Name);
            workflow.Build(builder);

            return builder.Build();
        }
    }
}