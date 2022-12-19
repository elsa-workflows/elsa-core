using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elsa.Secrets.Providers
{
    public interface ISecretsProvider
    {
        /// <summary>
        /// Retrieve all secrets for specific type.
        /// </summary>
        /// <param name="type">Type of secrets to retrieve.</param>
        /// <returns></returns>
        Task<ICollection<string>> GetSecrets(string type);
        Task<string> GetSecretById(string id);
        Task<string> GetSecretByName(string name);
        /// <summary>
        /// Retrieve single secret by name for specific type.
        /// </summary>
        /// <param name="type">Type of secret to retrieve.</param>
        /// <param name="name">Name of secret.</param>
        /// <returns></returns>
        Task<string?> GetSecrets(string type, string name);
        /// <summary>
        /// List all secrets for specific type as name value dictionary
        /// </summary>
        /// <param name="type">Type of secrets to retrieve.</param>
        /// <returns></returns>
        Task<IDictionary<string, string>> GetSecretsDictionaryAsync(string type);
    }
}
