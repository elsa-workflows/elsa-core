using System;
using System.Collections;
using System.Collections.Generic;
using Flowsharp.Activities;
using Flowsharp.Models;

namespace Flowsharp.Builders
{
    public class WorkflowBuilder
    {
        public WorkflowBuilder()
        {
            Activities = new List<IActivity>();
            Connections = new List<Connection>();
        }
        
        public IList<IActivity> Activities { get; }
        public IList<Connection> Connections { get; }
        
        public WorkflowBuilder AddActivity(IActivity activity, Action<ActivityWorkflowBuilder> builder = null)
        {
            Activities.Add(activity);
            builder?.Invoke(new ActivityWorkflowBuilder(this, activity));
            return this;
        }

        public Workflow Build()
        {
            return new Workflow(Activities, Connections);
        }
    }
}