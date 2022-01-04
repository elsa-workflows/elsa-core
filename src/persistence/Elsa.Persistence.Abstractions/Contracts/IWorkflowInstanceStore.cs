// using System.Collections.Generic;
// using System.Threading;
// using System.Threading.Tasks;
// using Elsa.Persistence.Entities;
//
// namespace Elsa.Persistence.Contracts;
//
// public interface IWorkflowInstanceStore
// {
//     Task<WorkflowInstance?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
//     Task SaveAsync(WorkflowInstance record, CancellationToken cancellationToken = default);
//     Task SaveManyAsync(IEnumerable<WorkflowInstance> records, CancellationToken cancellationToken = default);
// }