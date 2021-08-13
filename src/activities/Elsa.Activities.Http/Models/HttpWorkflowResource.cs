using Elsa.Services.Models;

namespace Elsa.Activities.Http.Models
{
    public record HttpWorkflowResource(IWorkflowBlueprint WorkflowBlueprint, IActivityBlueprint ActivityBlueprint, string WorkflowInstance);
}