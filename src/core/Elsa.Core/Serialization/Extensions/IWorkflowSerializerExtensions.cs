using Elsa.Services.Models;

namespace Elsa.Serialization.Extensions
{
    public static class WorkflowSerializerExtensions
    {
        public static string Serialize(this IWorkflowSerializer serializer, Workflow workflow, string format)
        {
            return serializer.Serialize(workflow.ToInstance(), format);
        }
    }
}