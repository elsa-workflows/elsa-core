using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Sample25.Activities
{
    /// <summary>
    /// Produces a single value.
    /// </summary>
    public class Channel : Activity
    {
        /// <summary>
        /// The ID of the sensor to observe.
        /// </summary>
        public string SensorId
        {
            get => GetState<string>();
            set => SetState(value);
        }

        // Execute only if we received data from the sensor being observed. 
        protected override bool OnCanExecute(WorkflowExecutionContext context) => context.Workflow.Input.ContainsKey(SensorId);

        // Halt workflow execution until sensor data is received.
        protected override ActivityExecutionResult OnExecute(WorkflowExecutionContext context) => Halt();

        protected override ActivityExecutionResult OnResume(WorkflowExecutionContext context)
        {
            // Read sensor output provided as workflow input. 
            var value = context.Workflow.Input.GetVariable<double>(SensorId);

            // Set the value as an output of this activity. 
            Output.SetVariable("Value", value);
            return Done();
        }
    }
}