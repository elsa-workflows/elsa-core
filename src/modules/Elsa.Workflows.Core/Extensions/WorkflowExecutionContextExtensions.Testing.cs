using Elsa.Workflows;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static partial class WorkflowExecutionContextExtensions
{
    extension(WorkflowExecutionContext context)
    {
        /// <summary>
        /// Returns true if the workflow is being executed by <see cref="IActivityTestRunner"/>.
        /// </summary>
        public bool IsTestExecution
            => context.TransientProperties
                .TryGetValue(IActivityTestRunner.TestExecutionKey, out var value) && value is true;

        /// <summary>
        /// Sets a test execution variable value on the workflow execution context.
        /// </summary>
        /// <typeparam name="T">The type of the variable value.</typeparam>
        /// <param name="variableName">The name of the variable to set.</param>
        /// <param name="value">The value to assign to the variable.</param>
        /// <returns>The current <see cref="WorkflowExecutionContext"/> for chaining.</returns>
        public WorkflowExecutionContext SetTestExecutionVariable<T>(string variableName, T? value)
        {
            var workflow = context.Workflow;
            workflow.SetTestVariable(variableName, value);
            return context;
        }
    }
}
