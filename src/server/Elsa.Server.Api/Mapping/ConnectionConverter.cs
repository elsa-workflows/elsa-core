using AutoMapper;
using Elsa.Server.Api.Endpoints.WorkflowRegistry;
using Elsa.Services.Models;

namespace Elsa.Server.Api.Mapping
{
    public class ConnectionConverter : ITypeConverter<IConnection, ConnectionModel>
    {
        public ConnectionModel Convert(IConnection source, ConnectionModel destination, ResolutionContext context) =>
            new()
            {
                SourceActivityId = source.Source.Activity.Id,
                TargetActivityId = source.Target.Activity.Id,
                Outcome = source.Source.Outcome
            };
    }
}