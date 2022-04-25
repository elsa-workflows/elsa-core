// using System.Collections.Generic;
// using System.Threading;
// using System.Threading.Tasks;
// using Elsa.Persistence.Entities;
//
// namespace Elsa.Persistence.Contracts;
//
// public interface IWorkflowTriggerStore
// {
//     Task SaveAsync(WorkflowTrigger record, CancellationToken cancellationToken = default);
//     Task SaveManyAsync(IEnumerable<WorkflowTrigger> records, CancellationToken cancellationToken = default);
//     Task<IEnumerable<WorkflowTrigger>> FindManyAsync(string name, string? hash, CancellationToken cancellationToken = default);
//     Task DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
//     Task ReplaceTriggersAsync(string workflowDefinitionId, IEnumerable<WorkflowTrigger> triggers, CancellationToken cancellationToken = default);
// }