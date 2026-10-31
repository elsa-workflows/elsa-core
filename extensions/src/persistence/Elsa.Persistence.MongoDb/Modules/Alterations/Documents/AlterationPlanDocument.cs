using Elsa.Alterations.Core.Enums;
using Elsa.Alterations.Core.Models;
using Elsa.Persistence.MongoDb.Common;

namespace Elsa.Persistence.MongoDb.Modules.Alterations.Documents;
internal class AlterationPlanDocument : Document
{
    public string Alterations { get; init; } = null!;

    public AlterationWorkflowInstanceFilter WorkflowInstanceFilter { get; init; } = null!;

    public AlterationPlanStatus Status { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? StartedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }
}