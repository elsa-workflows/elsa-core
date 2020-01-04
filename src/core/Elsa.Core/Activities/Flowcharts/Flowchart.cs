using System;
using System.Collections.Generic;
using Elsa.Activities.Containers;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Flowcharts
{
    public class Flowchart : Activity
    {
        public ICollection<IActivity> Activities
        {
            get => GetState<ICollection<IActivity>>(() => new List<IActivity>());
            set => SetState(value);
        }

        public ICollection<Connection> Connections
        {
            get => GetState<ICollection<Connection>>(() => new List<Connection>());
            set => SetState(value);
        }

        protected override IActivityExecutionResult OnExecute(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext)
        {
            // TODO: Implement scheduling.
            return Done();
        }
    }
}