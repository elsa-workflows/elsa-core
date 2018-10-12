using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.ActivityResults;
using Flowsharp.Models;

namespace Flowsharp.Descriptors
{
    public class ActivityDescriptor
    {
        public string Name { get; set; }

        /// <summary>
        /// Provides metadata about the specified activity.
        /// </summary>
        public Func<ActivityMetadataContext, CancellationToken, Task> GetMetadataAsync { get; set; }

        /// <summary>
        /// Returns a list of possible outcomes when the activity is executed.
        /// </summary>
        public Func<WorkflowExecutionContext, ActivityExecutionContext, IEnumerable<Outcome>> GetOutcomes { get; set; }

        /// <summary>
        /// Returns a value of whether the specified activity can execute.
        /// </summary>
        public Func<WorkflowExecutionContext, ActivityExecutionContext, CancellationToken, Task<bool>> CanExecuteAsync { get; set; }

        /// <summary>
        /// Executes the specified activity.
        /// </summary>
        public Func<WorkflowExecutionContext, ActivityExecutionContext, CancellationToken, Task<ActivityExecutionResult>> ExecuteActivityAsync { get; set; }

        /// <summary>
        /// Resumes the specified activity.
        /// </summary>
        public Func<WorkflowExecutionContext, ActivityExecutionContext, CancellationToken, Task<ActivityExecutionResult>> ResumeActivityAsync { get; set; }

        /// <summary>
        /// Executes before a workflow starts or resumes, giving activities an opportunity to read and store any values of interest.
        /// </summary>
        public Func<WorkflowExecutionContext, IDictionary<string, object>, CancellationToken, Task> ReceiveInputAsync { get; set; }

        /// <summary>
        /// Executes when a workflow is about to start.
        /// </summary>
        public Func<WorkflowExecutionContext, CancellationToken, Task> WorkflowStartingAsync { get; set; }

        /// <summary>
        /// Executes when a workflow has started.
        /// </summary>
        public Func<WorkflowExecutionContext, CancellationToken, Task> WorkflowStartedAsync { get; set; }

        /// <summary>
        /// Executes when a workflow is about to be resumed.
        /// </summary>
        public Func<WorkflowExecutionContext, CancellationToken, Task> WorkflowResumingAsync { get; set; }

        /// <summary>
        /// Executes when a workflow is resumed.
        /// </summary>
        public Func<WorkflowExecutionContext, CancellationToken, Task> WorkflowResumedAsync { get; set; }

        /// <summary>
        /// Executes when an activity is about to be executed.
        /// </summary>
        /// <param name="activity">The activity for which the event is invoked. This is not necessarily the activity that is about to be executed.</param>
        /// <param name="activityContext">The activity context containing the activity that is the subject of the event.</param>
        public Func<WorkflowExecutionContext, ActivityExecutionContext, CancellationToken, Task> OnActivityExecutingAsync { get; set; }

        /// <summary>
        /// Called on each activity when an activity has been executed.
        /// </summary>
        /// <param name="activity">The activity for which the event is invoked. This is not necessarily the activity that is about to be executed.</param>
        /// <param name="activityContext">The activity context containing the activity that is the subject of the event.</param>
        public Func<WorkflowExecutionContext, ActivityExecutionContext, CancellationToken, Task> OnActivityExecutedAsync { get; set; }
    }
}
