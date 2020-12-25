using System;
using Elsa.Client.Models;

namespace ElsaDashboard.Application.Models
{
    public class WorkflowModelChangedEventArgs : EventArgs
    {
        public WorkflowModel WorkflowModel { get; }

        public WorkflowModelChangedEventArgs(WorkflowModel workflowModel)
        {
            WorkflowModel = workflowModel;
        }
    }
}