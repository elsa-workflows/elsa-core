using System;
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
        Task<ICollection<string>> GetSecretsAsync(string type);
        /// <summary>
        /// Retrieve a single secret by it's unique id.
        /// </summary>
        /// <param name="id">Unique secret id</param>
        /// <returns></returns>
        Task<string?> GetSecretByIdAsync(string id);
        /// <summary>
        /// Retrieve first secret matching name, ordered by secret type.
        /// </summary>
        /// <param name="name">Name of secret</param>
        /// <returns></returns>
        Task<string?> GetSecretByNameAsync(string name);
        /// <summary>
        /// Retrieve single secret by name for specific type.
        /// </summary>
        /// <param name="type">Type of secret to retrieve.</param>
        /// <param name="name">Name of secret.</param>
        /// <returns></returns>
        Task<string?> GetSecretByNameAsync(string type, string name);
        /// <summary>
        /// List all secrets for specific type as name value dictionary
        /// </summary>
        /// <param name="type">Type of secrets to retrieve.</param>
        /// <returns></returns>
        Task<IDictionary<string, string>> GetSecretsDictionaryAsync(string type);
        [Obsolete("Use GetSecretsDictionaryAsync instead!", true)]
        Task<IDictionary<string, string>> GetSecretsForSelectListAsync(string type);
        
    }
}
