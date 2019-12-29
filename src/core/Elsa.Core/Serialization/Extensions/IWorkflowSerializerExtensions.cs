using System;
using Elsa.Services.Models;

namespace Elsa.Serialization.Extensions
{
    public static class WorkflowSerializerExtensions
    {
        public static string Serialize(this IWorkflowSerializer serializer, ProcessInstance workflow, string format)
        {
            //return serializer.Serialize(workflow.ToInstance(), format);
            throw new NotImplementedException();
        }
    }
}