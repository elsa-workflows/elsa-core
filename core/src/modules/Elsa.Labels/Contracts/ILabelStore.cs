using Elsa.Common.Models;
using Elsa.Labels.Entities;

namespace Elsa.Labels.Contracts;

public interface ILabelStore
{
    Task SaveAsync(Label record, CancellationToken cancellationToken = default);
    Task SaveManyAsync(IEnumerable<Label> records, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<long> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
    Task<Label?> FindByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<Page<Label>> ListAsync(PageArgs? pageArgs = default, CancellationToken cancellationToken = default);
    Task<IEnumerable<Label>> FindManyByIdAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
}