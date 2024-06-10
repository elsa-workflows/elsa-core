using Elsa.Alterations.Core.Enums;
using Elsa.Alterations.Core.Models;

namespace Elsa.MongoDb.Modules.Alterations.Documents;
internal class AlterationPlanDocument
{
    public string Id { get; init; } = default!;

    public string Alterations { get; init; } = default!;

    public AlterationWorkflowInstanceFilter WorkflowInstanceFilter { get; init; } = default!;

    public AlterationPlanStatus Status { get; init; } = default!;

    public DateTimeOffset CreatedAt { get; init; } = default!;

    public DateTimeOffset? StartedAt { get; init; } = default!;

    public DateTimeOffset? CompletedAt { get; init; } = default!;
}
