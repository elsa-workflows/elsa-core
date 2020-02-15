using System;
using Elsa.Models;

namespace Elsa.Serialization.Extensions
{
    public static class WorkflowSerializerExtensions
    {
        public static string Serialize(this ITokenSerializer serializer, WorkflowInstance workflow, string format)
        {
            //return serializer.Serialize(workflow.ToInstance(), format);
            throw new NotImplementedException();
        }
    }
}