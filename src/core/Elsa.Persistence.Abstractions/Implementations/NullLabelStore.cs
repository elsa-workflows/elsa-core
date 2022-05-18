using Elsa.Persistence.Entities;
using Elsa.Persistence.Models;
using Elsa.Persistence.Services;

namespace Elsa.Persistence.Implementations;

public class NullLabelStore : ILabelStore
{
    public Task SaveAsync(Label record, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task SaveManyAsync(IEnumerable<Label> records, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<int> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default) => Task.FromResult(0);
    public Task<Label?> FindByIdAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult<Label?>(null);
    public Task<Page<Label>> ListAsync(PageArgs? pageArgs = default, CancellationToken cancellationToken = default) => Task.FromResult(new Page<Label>(Array.Empty<Label>(), 0));
    public Task<IEnumerable<Label>> FindManyByIdAsync(IEnumerable<string> ids, CancellationToken cancellationToken) => Task.FromResult(Enumerable.Empty<Label>());
}