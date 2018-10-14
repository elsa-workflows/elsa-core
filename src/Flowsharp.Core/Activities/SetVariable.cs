using System;
using Flowsharp.ActivityResults;
using Flowsharp.Models;

namespace Flowsharp.Activities
{
    public class SetVariable : Activity
    {
        private readonly Func<WorkflowExecutionContext, ActivityExecutionContext, object> valueProvider;

        public SetVariable()
        {
        }

        public SetVariable(string name, Func<WorkflowExecutionContext, ActivityExecutionContext, object> valueProvider) : this(name, default(object))
        {
            VariableName = name;
            this.valueProvider = valueProvider;
        }
        
        public SetVariable(string name, object value)
        {
            VariableName = name;
            Value = value;
        }
        
        public string VariableName { get; set; }
        public object Value { get; set; }

        protected override ActivityExecutionResult Execute(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext)
        {
            var value = valueProvider(workflowContext, activityContext);
            workflowContext.CurrentScope.SetVariable(VariableName, value);
            return ActivateEndpoint();
        }
    }
}