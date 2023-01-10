using System.Threading;
using System.Threading.Tasks;
using Elsa.Secrets.Models;
using Elsa.Secrets.Persistence;
using Elsa.Secrets.Persistence.Specifications;

namespace Elsa.Secrets.Extensions
{
    public static class SecretsStoreExtensions
    {
        public static Task<Secret?> FindByIdAsync(
           this ISecretsStore store,
           string id,
           CancellationToken cancellationToken = default) =>
           store.FindAsync(new SecretsIdSpecification(id), cancellationToken);

    }
}
