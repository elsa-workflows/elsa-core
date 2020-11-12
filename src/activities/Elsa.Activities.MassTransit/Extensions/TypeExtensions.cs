using Elsa.Activities.MassTransit.Consumers;
using MassTransit;
using System;

namespace Elsa.Activities.MassTransit.Extensions
{
    internal static class TypeExtensions 
    {
        public static void MapEndpointConvention(this Type messageType, Uri destinationAddress)
        {
            var method = typeof(EndpointConvention).GetMethod("Map", new[] { typeof(Uri) });
            var generic = method.MakeGenericMethod(messageType);
            generic.Invoke(null, new object[] { destinationAddress });
        }

        public static Type CreateConsumerType(this Type messageType)
        {
            return typeof(WorkflowConsumer<>).MakeGenericType(messageType);
        }
    }
}