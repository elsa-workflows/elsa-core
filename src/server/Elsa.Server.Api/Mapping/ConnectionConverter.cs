using AutoMapper;
using Elsa.Server.Api.Endpoints.WorkflowRegistry;
using Elsa.Services.Models;

namespace Elsa.Server.Api.Mapping
{
    public class ConnectionConverter : ITypeConverter<IConnection, ConnectionModel?>
    {
        public ConnectionModel Convert(IConnection source, ConnectionModel? destination, ResolutionContext context)
        {
            destination ??= new ConnectionModel();

            destination.SourceActivityId = source.Source.Activity.Id;
            destination.TargetActivityId = source.Target.Activity.Id;
            destination.Outcome = source.Source.Outcome;

            return destination;
        }
    }
}