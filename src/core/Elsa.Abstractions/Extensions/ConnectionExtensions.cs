using System.Collections.Generic;
using Elsa.Models;

namespace Elsa.Extensions
{
    public static class ConnectionExtensions
    {
        public static void Add(this ICollection<Connection> connections, IActivity source, IActivity destination, string sourceEndpointName = EndpointNames.Done)
        {
            connections.Add(new Connection(source, destination, sourceEndpointName));
        }
    }
}