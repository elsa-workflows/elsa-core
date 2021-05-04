using System;

namespace ElsaDashboard.Application.Models
{
    public class WorkflowModelChangedEventArgs : EventArgs
    {
        public WorkflowModel WorkflowModel { get; }
        public WorkflowModelChangedEventArgs(WorkflowModel workflowModel) => WorkflowModel = workflowModel;
    }
}