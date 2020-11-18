using System;
using ElsaDashboard.Application.Models;

namespace ElsaDashboard.Application.Shared
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