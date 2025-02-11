using Elsa.Alterations.Core.Enums;
using Elsa.Alterations.Core.Models;
using Elsa.MongoDb.Common;

namespace Elsa.MongoDb.Modules.Alterations.Documents;
internal class AlterationPlanDocument : Document
{
    public string Alterations { get; init; } = default!;

    public AlterationWorkflowInstanceFilter WorkflowInstanceFilter { get; init; } = default!;

    public AlterationPlanStatus Status { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? StartedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }
}