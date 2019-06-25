using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services.Extensions
{
    public static class WorkflowBuilderExtensions
    {
        public static WorkflowDefinition Build<T>(this IWorkflowBuilder builder) where T:IWorkflow, new()
        {
            var workflow = new T();
            workflow.Build(builder);

            if (string.IsNullOrWhiteSpace(builder.Id))
            {
                builder.Id = typeof(T).Name;
            }
            
            return builder.Build();
        }
    }
}