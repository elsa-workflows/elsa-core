using System.Collections.Generic;
using Elsa.Models;
using MediatR;

namespace Elsa.Events
{
    public class ManyWorkflowInstancesAdded : INotification
    {
        public ManyWorkflowInstancesAdded(IEnumerable<WorkflowInstance> workflowInstances)
        {
            WorkflowInstances = workflowInstances;
        }

        public IEnumerable<WorkflowInstance> WorkflowInstances { get; }
    }
}