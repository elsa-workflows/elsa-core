using Elsa.Abstractions.MultiTenancy;
using Elsa.Models;

namespace Elsa.Events
{
    public class WorkflowDefinitionPublished : WorkflowDefinitionNotification
    {
        public Tenant Tenant { get; }

        public WorkflowDefinitionPublished(WorkflowDefinition workflowDefinition, Tenant tenant) : base(workflowDefinition)
        {
            Tenant = tenant;
        }
    }
}