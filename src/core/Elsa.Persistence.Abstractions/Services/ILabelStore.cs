using Elsa.Persistence.Entities;
using Elsa.Persistence.Models;

namespace Elsa.Persistence.Services;

public interface ILabelStore
{
    Task SaveAsync(Label record, CancellationToken cancellationToken = default);
    Task SaveManyAsync(IEnumerable<Label> records, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<int> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
    Task<Label?> FindByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<Page<Label>> ListAsync(PageArgs? pageArgs = default, CancellationToken cancellationToken = default);
}