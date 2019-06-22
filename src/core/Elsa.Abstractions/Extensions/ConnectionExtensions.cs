using System.Collections.Generic;
using Elsa.Models;
using Elsa.Serialization.Models;

namespace Elsa.Extensions
{
    public static class ConnectionExtensions
    {
        public static void Add(this ICollection<Connection> connections, string sourceActivityId, string targetActivityId, string sourceEndpointName = EndpointNames.Done)
        {
            connections.Add(new Connection(sourceActivityId, targetActivityId, sourceEndpointName));
        }
    }
}