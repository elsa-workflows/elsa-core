using Elsa.Workflows;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Xunit;

namespace Elsa.Testing.Shared;

/// <summary>
/// Provides assertion methods for <see cref="RunWorkflowResult"/> to facilitate Journal-based testing.
/// These methods integrate with xUnit's assertion framework.
/// </summary>
[PublicAPI]
public static class RunWorkflowResultAssertions
{
    /// <param name="result">The workflow result.</param>
    extension(RunWorkflowResult result)
    {
        /// <summary>
        /// Asserts that the specified activity was executed (present in the journal).
        /// </summary>
        /// <param name="activity">The activity that should have been executed.</param>
        public void AssertActivityExecuted(IActivity activity)
        {
            var context = result.GetActivityContext(activity);
            Assert.NotNull(context);
        }

        /// <summary>
        /// Asserts that the specified activity was not executed (not present in the journal).
        /// </summary>
        /// <param name="activity">The activity that should not have been executed.</param>
        public void AssertActivityNotExecuted(IActivity activity)
        {
            var context = result.GetActivityContext(activity);
            Assert.Null(context);
        }

        /// <summary>
        /// Asserts that the specified activity completed successfully.
        /// </summary>
        /// <param name="activity">The activity that should have completed.</param>
        public void AssertActivityCompleted(IActivity activity)
        {
            var context = result.GetActivityContext(activity);
            Assert.NotNull(context);
            Assert.Equal(ActivityStatus.Completed, context.Status);
        }

        /// <summary>
        /// Asserts that the specified activity has the expected status.
        /// </summary>
        /// <param name="activity">The activity to check.</param>
        /// <param name="expectedStatus">The expected status.</param>
        public void AssertActivityStatus(IActivity activity, ActivityStatus expectedStatus)
        {
            var context = result.GetActivityContext(activity);
            Assert.NotNull(context);
            Assert.Equal(expectedStatus, context.Status);
        }

        /// <summary>
        /// Asserts that all specified activities were executed.
        /// </summary>
        /// <param name="activities">The activities that should have been executed.</param>
        public void AssertActivitiesExecuted(params IActivity[] activities)
        {
            foreach (var activity in activities)
                result.AssertActivityExecuted(activity);
        }

        /// <summary>
        /// Asserts that all specified activities were not executed.
        /// </summary>
        /// <param name="activities">The activities that should not have been executed.</param>
        public void AssertActivitiesNotExecuted(params IActivity[] activities)
        {
            foreach (var activity in activities)
                result.AssertActivityNotExecuted(activity);
        }

        /// <summary>
        /// Asserts that all specified activities completed successfully.
        /// </summary>
        /// <param name="activities">The activities that should have completed.</param>
        public void AssertActivitiesCompleted(params IActivity[] activities)
        {
            foreach (var activity in activities)
                result.AssertActivityCompleted(activity);
        }

        /// <summary>
        /// Asserts that the specified activity was executed exactly the expected number of times.
        /// </summary>
        /// <param name="activity">The activity to check.</param>
        /// <param name="expectedCount">The expected execution count.</param>
        public void AssertActivityExecutionCount(IActivity activity, int expectedCount)
        {
            var actualCount = result.GetExecutionCount(activity);
            Assert.Equal(expectedCount, actualCount);
        }

        /// <summary>
        /// Asserts that the workflow completed successfully.
        /// </summary>
        public void AssertWorkflowCompleted()
        {
            Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
            Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
        }

        /// <summary>
        /// Asserts that the workflow has the expected status and substatus.
        /// </summary>
        /// <param name="expectedStatus">The expected workflow status.</param>
        /// <param name="expectedSubStatus">The expected workflow substatus.</param>
        public void AssertWorkflowStatus(WorkflowStatus expectedStatus, WorkflowSubStatus expectedSubStatus)
        {
            Assert.Equal(expectedStatus, result.WorkflowState.Status);
            Assert.Equal(expectedSubStatus, result.WorkflowState.SubStatus);
        }
    }
}
