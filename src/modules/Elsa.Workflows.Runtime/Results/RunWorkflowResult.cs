// using Elsa.Workflows.Helpers;
// using Elsa.Workflows.Models;
//
// namespace Elsa.Workflows.Runtime.Results;
//
// /// <summary>
// /// Represents the result of executing a workflow.
// /// </summary>
// /// <param name="WorkflowInstanceId">The ID of the workflow instance.</param>
// /// <param name="BookmarksDiff">A diff of the bookmarks.</param>
// /// <param name="Status">The status of the workflow.</param>
// /// <param name="SubStatus">The sub status of the workflow.</param>
// /// <param name="Incidents">A collection of incidents that may have occurred during execution.</param>
// public record RunWorkflowResult(string WorkflowInstanceId, Diff<BookmarkInfo> BookmarksDiff, WorkflowStatus Status, WorkflowSubStatus SubStatus, ICollection<ActivityIncident> Incidents);