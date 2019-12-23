using Elsa.Services.Models;

namespace Elsa.Services.Extensions
{
    public static class WorkflowBuilderExtensions
    {
        public static WorkflowBlueprint Build<T>(this IWorkflowBuilder builder) where T : IWorkflow, new()
        {
            var workflow = new T();
            builder.WithId(typeof(T).Name);
            workflow.Build(builder);

            return builder.Build();
        }
    }
}