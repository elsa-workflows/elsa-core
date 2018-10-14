using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Activities;

namespace Flowsharp.Models
{
    public class WorkflowExecutionContext
    {
        public WorkflowExecutionContext(Workflow workflow, WorkflowStatus status = WorkflowStatus.Idle)
        {
            Workflow = workflow;
            Status = status;
            IsFirstPass = true;            
            scheduledActivities = new Stack<IActivity>();
            scopes = new Stack<WorkflowExecutionScope>();
            
            BeginScope();
        }
        
        private readonly Stack<IActivity> scheduledActivities;
        private readonly Stack<WorkflowExecutionScope> scopes;

        public Workflow Workflow { get; }
        public WorkflowStatus Status { get; set; }
        public bool HasScheduledActivities => scheduledActivities.Any();
        public bool IsFirstPass { get; set; }
        public IActivity CurrentActivity { get; private set; }
        public WorkflowExecutionScope CurrentScope { get; private set; }

        public void BeginScope()
        { 
            scopes.Push(CurrentScope = new WorkflowExecutionScope());
        }

        public void EndScope()
        {
            scopes.Pop();
            CurrentScope = scopes.Peek();
        }

        public void ScheduleActivity(IActivity activity)
        {
            scheduledActivities.Push(activity);
        }
        
        public IActivity PopScheduledActivity()
        {
            CurrentActivity = scheduledActivities.Pop();
            return CurrentActivity;
        }
        
        public void SetReturnValue(object value)
        {
            CurrentScope.ReturnValue = value;
        }

        public void Fault(Exception exception, IActivity activity)
        {
            throw new NotImplementedException();
        }

        public Task HaltAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Finish()
        {
            Status = WorkflowStatus.Finished;
        }
        
        public virtual void ScheduleNextActivities(WorkflowExecutionContext workflowContext, SourceEndpoint endpoint)
        {
            var completedActivity = workflowContext.CurrentActivity;
            var connections = Workflow.Connections.Where(x => x.Source.Activity == completedActivity && x.Source.Name == endpoint.Name);

            foreach (var connection in connections)
            {
                workflowContext.ScheduleActivity(connection.Target.Activity);
            }
        }
    }
}
