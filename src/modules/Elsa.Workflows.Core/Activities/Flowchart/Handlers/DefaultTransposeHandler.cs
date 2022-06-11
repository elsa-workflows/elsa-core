using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Activities.Flowchart.Transposition.Services;

namespace Elsa.Workflows.Core.Activities.Flowchart.Handlers;

public class DefaultTransposeHandler : ITransposeHandler
{
    public bool Transpose(TransposeContext context)
    {
        var (connection, sourceDescriptor, _) = context;
        var (source, target) = connection;
        var matchingTargetPort = sourceDescriptor.OutPorts.FirstOrDefault(x => x.Name == connection.SourcePort);

        if (matchingTargetPort == null)
            return false;

        // Assign the target activity directly to the outbound port of the source activity.
        source.SetPropertyValue(connection.SourcePort!, target);

        return true;
    }
}