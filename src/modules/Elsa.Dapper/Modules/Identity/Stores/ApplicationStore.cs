using Elsa.Dapper.Extensions;
using Elsa.Dapper.Models;
using Elsa.Dapper.Modules.Identity.Records;
using Elsa.Dapper.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;

namespace Elsa.Dapper.Modules.Identity.Stores;

/// <summary>
/// A Dapper implementation of <see cref="IApplicationStore"/>.
/// </summary>
internal class DapperApplicationStore(Store<ApplicationRecord> store) : IApplicationStore
{
    /// <inheritdoc />
    public async Task SaveAsync(Application application, CancellationToken cancellationToken = default)
    {
        var record = Map(application);
        await store.SaveAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(ApplicationFilter filter, CancellationToken cancellationToken = default) => await store.DeleteAsync(q => ApplyFilter(q, filter), cancellationToken);

    /// <inheritdoc />
    public async Task<Application?> FindAsync(ApplicationFilter filter, CancellationToken cancellationToken = default)
    {
        var record = await store.FindAsync(query => ApplyFilter(query, filter), cancellationToken);
        return record == null ? null : Map(record);
    }

    private void ApplyFilter(ParameterizedQuery query, ApplicationFilter filter)
    {
        query
            .Is(nameof(ApplicationRecord.Id), filter.Id)
            .Is(nameof(ApplicationRecord.ClientId), filter.ClientId)
            .Is(nameof(ApplicationRecord.Name), filter.Name)
            ;
    }

    private ApplicationRecord Map(Application source) =>
        new()
        {
            Id = source.Id,
            ClientId = source.ClientId,
            HashedClientSecret = source.HashedClientSecret,
            HashedClientSecretSalt = source.HashedClientSecretSalt,
            Name = source.Name,
            HashedApiKey = source.HashedApiKey,
            HashedApiKeySalt = source.HashedApiKeySalt,
            Roles = string.Join(',', source.Roles),
            TenantId = source.TenantId
        };

    private Application Map(ApplicationRecord source) =>
        new()
        {
            Id = source.Id,
            ClientId = source.ClientId,
            HashedClientSecret = source.HashedClientSecret,
            HashedClientSecretSalt = source.HashedClientSecretSalt,
            Name = source.Name,
            HashedApiKey = source.HashedApiKey,
            HashedApiKeySalt = source.HashedApiKeySalt,
            Roles = source.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries),
            TenantId = source.TenantId
        };
}