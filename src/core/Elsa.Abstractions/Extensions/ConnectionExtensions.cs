using System.Collections.Generic;
using Elsa.Activities.Workflows.Models;
using Elsa.Services.Models;

namespace Elsa.Activities.Workflows.Extensions
{
    public static class ConnectionExtensions
    {
        public static void Add(this ICollection<Connection> connections, IActivity sourceActivity, IActivity targetActivity, string? sourceEndpointName = null)
        {
            connections.Add(new Connection(sourceActivity, targetActivity, sourceEndpointName));
        }
    }
}