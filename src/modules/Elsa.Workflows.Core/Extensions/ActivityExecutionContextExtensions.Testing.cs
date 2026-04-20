using Elsa.Workflows;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static partial class ActivityExecutionContextExtensions
{
    extension(ActivityExecutionContext context)
    {
        /// <summary>
        /// Returns true if the activity is being executed by <see cref="IActivityTestRunner"/>.
        /// </summary>
        public bool IsTestExecution
            => context.WorkflowExecutionContext.TransientProperties
                .TryGetValue(IActivityTestRunner.TestExecutionKey, out var value) && value is true;

        /// <summary>
        /// Sets a test execution variable value on the activity execution context.
        /// </summary>
        /// <typeparam name="T">The type of the variable value.</typeparam>
        /// <param name="variableName">The name of the variable to set.</param>
        /// <param name="value">The value to assign to the variable.</param>
        /// <returns>The current <see cref="ActivityExecutionContext"/> for chaining.</returns>
        public ActivityExecutionContext SetTestExecutionVariable<T>(string variableName, T? value)
        {
            var executionContext = context.WorkflowExecutionContext;
            var workflow = executionContext.Workflow;
            workflow.SetTestVariable(variableName, value);
            return context;
        }
    }
}
