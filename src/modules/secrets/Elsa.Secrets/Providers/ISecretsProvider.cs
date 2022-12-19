using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elsa.Secrets.Providers
{
    public interface ISecretsProvider
    {
        Task<string> GetSecretById(string id);
        Task<string> GetSecretByName(string name);
        Task<ICollection<string>> GetSecrets(string type);
        Task<ICollection<(string, string)>> GetSecretsForSelectListAsync(string type);
    }
}
