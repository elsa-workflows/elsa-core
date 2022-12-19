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
        /// <summary>
        /// Retrieve a single secret by it's unique id.
        /// </summary>
        /// <param name="id">Unique secret id</param>
        /// <returns></returns>
        Task<string?> GetSecretById(string id);
        /// <summary>
        /// Retrieve a single secret by it's name.
        /// </summary>
        /// <param name="name">Name of secret</param>
        /// <returns></returns>
        Task<string?> GetSecretByName(string name);
        /// <summary>
        /// Retrieve single secret by name for specific type.
        /// </summary>
        /// <param name="type">Type of secret to retrieve.</param>
        /// <param name="name">Name of secret.</param>
        /// <returns></returns>
        Task<string?> GetSecret(string type, string name);
        /// <summary>
        /// List all secrets for specific type as name value dictionary
        /// </summary>
        /// <param name="type">Type of secrets to retrieve.</param>
        /// <returns></returns>
        Task<IDictionary<string, string>> GetSecretsDictionaryAsync(string type);
    }
}
