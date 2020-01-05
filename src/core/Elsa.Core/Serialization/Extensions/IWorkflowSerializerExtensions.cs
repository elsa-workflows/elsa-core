using System;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Serialization.Extensions
{
    public static class WorkflowSerializerExtensions
    {
        public static string Serialize(this IWorkflowSerializer serializer, WorkflowInstance workflow, string format)
        {
            //return serializer.Serialize(workflow.ToInstance(), format);
            throw new NotImplementedException();
        }
    }
}