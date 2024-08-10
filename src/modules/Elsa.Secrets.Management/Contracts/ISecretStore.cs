using Elsa.Common.Models;
using Elsa.Secrets.Management.Entities;

namespace Elsa.Secrets.Management.Contracts;

public interface ISecretStore
{
    Task<Page<Secret>> FindManyAsync(SecretFilter filter, SecretOrder<DateTimeOffset> order, PageArgs pageArgs, CancellationToken cancellationToken = default);
}