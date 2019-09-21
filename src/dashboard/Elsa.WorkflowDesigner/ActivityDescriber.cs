using System;
using Elsa.WorkflowDesigner.Models;

namespace Elsa.WorkflowDesigner
{
    public static class ActivityDescriber
    {
        public static ActivityDefinition Describe(Type activityType)
        {
            return new ActivityDefinition();
        }
    }
}