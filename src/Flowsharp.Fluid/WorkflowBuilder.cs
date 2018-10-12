using System;
using System.Collections.Generic;
using Flowsharp.Models;

namespace Flowsharp.Fluid
{
    public class WorkflowBuilder
    {
        public WorkflowBuilder()
        {
            activityTypes = new List<ActivityType>();
        }
        
        private IList<ActivityType> activityTypes;
         
    }
}