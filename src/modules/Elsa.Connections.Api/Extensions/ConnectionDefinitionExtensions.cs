using Elsa.Connections.Models;
using Elsa.Connections.Persistence.Entities;

namespace Elsa.Connections.Api.Extensions;

public static class ConnectionDefinitionExtensions
{
    public static ConnectionModel ToModel(this ConnectionDefinition entity)
    {
        return new()
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            ConnectionType = entity.ConnectionType,
            ConnectionConfiguration = entity.ConnectionConfiguration,
        };
    }
}