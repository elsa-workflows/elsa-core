using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Flowsharp.Models
{
    public class WorkflowExecutionContext
    {
        public WorkflowExecutionContext(Workflow workflow)
        {
            Workflow = workflow;
            IsFirstPass = true;            
            scheduledActivities = new Stack<IActivity>();
        }
        
        private readonly Stack<IActivity> scheduledActivities;

        public Workflow Workflow { get; }
        public bool HasScheduledActivities => scheduledActivities.Any();
        public bool IsFirstPass { get; set; }
        public IActivity CurrentActivity { get; private set; }
        public WorkflowExecutionScope CurrentScope
        {
            get => Workflow.CurrentScope;
            set => Workflow.CurrentScope = value;
        }

        public void BeginScope()
        { 
            Workflow.Scopes.Push(CurrentScope = new WorkflowExecutionScope());
        }

        public void EndScope()
        {
            Workflow.Scopes.Pop();
            CurrentScope = Workflow.Scopes.Peek();
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
        
        public void SetLastResult(object value)
        {
            CurrentScope.LastResult = value;
        }

        public void Fault(Exception exception, IActivity activity)
        {
            throw new NotImplementedException();
        }

        public Task HaltAsync(CancellationToken cancellationToken)
        {
            var activity = CurrentActivity;
            if (!Workflow.BlockingActivities.Contains(activity))
            {
                Workflow.BlockingActivities.Add(activity);
            }

            Workflow.Status = WorkflowStatus.Halted;
            return Task.CompletedTask;
        }

        public void Finish()
        {
            Workflow.Status = WorkflowStatus.Finished;
        }
        
        public void ScheduleNextActivities(WorkflowExecutionContext workflowContext, SourceEndpoint endpoint)
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
