using System;
using Flowsharp.Models;

namespace Flowsharp.Serialization.Extensions
{
    public static class WorkflowSerializerExtensions
    {
        public static Workflow Clone(this IWorkflowSerializer workflowSerializer, Workflow workflow)
        {
            var json = workflowSerializer.Serialize(workflow);
            return workflowSerializer.Deserialize(json);
        }
        
        public static Workflow CreateOffspring(this IWorkflowSerializer workflowSerializer, Workflow parent)
        {
            var child = workflowSerializer.Clone(parent);
            child.Metadata.ParentId = parent.Metadata.Id;
            child.Metadata.Id = Guid.NewGuid().ToString();
            return child;
        }
    }
}