using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Http.Models;

public record HttpWorkflowLookupResult(Workflow? Workflow, ICollection<StoredTrigger> Triggers);