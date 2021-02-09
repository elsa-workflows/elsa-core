using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Client.Models;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Events
{
    public class DisplayingWorkflowInstanceRecord : INotification
    {
        public WorkflowInstanceSummary WorkflowInstance { get; }
        public Func<Task> Reload { get; }
        public ICollection<RenderFragment> ContextMenuItems { get; set; } = new List<RenderFragment>();

        public DisplayingWorkflowInstanceRecord(WorkflowInstanceSummary workflowInstance, Func<Task> reload)
        {
            WorkflowInstance = workflowInstance;
            Reload = reload;
        }
    }
}