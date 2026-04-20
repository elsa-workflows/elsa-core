using Elsa.Workflows;

namespace Elsa.Extensions;

public static class TestRunnerExtensions
{
    private static readonly object s_testExecutionKey = new();

    extension(IActivityTestRunner)
    {
        /// <summary>
        /// Gets the name of the custom property used to store variable test values in the workflow's custom properties.
        /// </summary>
        public static string VariableTestValuesPropertyName => "VariableTestValues";

        /// <summary>
        /// Gets the unique object that is used to mark a <see cref="WorkflowExecutionContext.TransientProperties"/> entry to 
        /// indicate that the workflow is being executed by the test runner.
        /// </summary>
        public static object TestExecutionKey => s_testExecutionKey;
    }
}