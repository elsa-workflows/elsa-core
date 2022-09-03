using Elsa.Persistence.Common.Entities;

namespace Elsa.Workflows.Persistence.Entities;

public class WorkflowStateRecord : Entity
{
    public string StateId { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
}