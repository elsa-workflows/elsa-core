using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Connections.Models;
using Elsa.Connections.Persistence.Entities;

namespace Elsa.Connections.Api.Extensions;
public static class ConnectionDefinitionExtensions
{
    public static ConnectionModel ToModel(this ConnectionDefinition entity)
    {
        return new ConnectionModel
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            ConnectionType = entity.ConnectionType,
            ConnectionConfiguration = entity.ConnectionConfiguration,

        };
    }
}
