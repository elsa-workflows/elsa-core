using System;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Flowsharp.Serialization.Formatters;

namespace Flowsharp.Serialization.Extensions
{
    public static class WorkflowSerializerExtensions
    {
        public static async Task<Workflow> CloneAsync(this IWorkflowSerializer workflowSerializer, Workflow workflow, CancellationToken cancellationToken)
        {
            var format = JsonTokenFormatter.FormatName;
            var json = await workflowSerializer.SerializeAsync(workflow, format, cancellationToken);
            return await workflowSerializer.DeserializeAsync(json, format, cancellationToken);
        }
        
        public static async Task<Workflow> DeriveAsync(this IWorkflowSerializer workflowSerializer, Workflow parent, CancellationToken cancellationToken)
        {
            var child = await workflowSerializer.CloneAsync(parent, cancellationToken);
            child.Metadata.ParentId = parent.Metadata.Id;
            child.Metadata.Id = Guid.NewGuid().ToString();
            return child;
        }
    }
}