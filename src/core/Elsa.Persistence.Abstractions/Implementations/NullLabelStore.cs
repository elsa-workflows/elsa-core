using Elsa.Persistence.Entities;
using Elsa.Persistence.Services;

namespace Elsa.Persistence.Implementations;

public class NullLabelStore : ILabelStore
{
    public Task SaveAsync(Label record, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task SaveManyAsync(IEnumerable<Label> records, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<int> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default) => Task.FromResult(0);
}