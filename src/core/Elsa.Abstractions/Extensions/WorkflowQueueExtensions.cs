// using System.Threading;
// using System.Threading.Tasks;
// using Elsa.Bookmarks;
// using Elsa.Services;
//
// namespace Elsa
// {
//     public static class WorkflowQueueExtensions
//     {
//         public static Task EnqueueWorkflowsAsync<T>(
//             this IWorkflowQueue workflowQueue,
//             IBookmark bookmark,
//             string? tenantId,
//             object? input = default,
//             string? correlationId = default,
//             string? contextId = default,
//             CancellationToken cancellationToken = default) where T : IActivity =>
//             workflowQueue.EnqueueWorkflowsAsync(typeof(T).Name, bookmark, tenantId, input, correlationId, contextId, cancellationToken);
//     }
// }