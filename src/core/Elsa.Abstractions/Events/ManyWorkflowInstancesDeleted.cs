using System.Collections.Generic;
using Elsa.Models;
using MediatR;

namespace Elsa.Events
{
    public class ManyWorkflowInstancesDeleted : INotification
    {
        public ManyWorkflowInstancesDeleted(IEnumerable<WorkflowInstance> workflowInstances)
        {
            WorkflowInstances = workflowInstances;
        }

        public IEnumerable<WorkflowInstance> WorkflowInstances { get; }
    }
}