using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Workflows.Activities.Flowchart.Contracts;

public interface IJoinHintProvider
{
    JoinKind DefaultJoinKind { get; }
}