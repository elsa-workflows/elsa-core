using Elsa.Persistence;
using Elsa.Secrets.Models;

namespace Elsa.Secrets.Persistence
{
    public interface ISecretsStore : IStore<Secret>
    {
    }
}
