using Elsa.Secrets.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Secrets.Manager
{
    public interface ISecretsManager
    {
        /// <summary>
        /// Retrieve single secret by its unique ID.
        /// </summary>
        /// <param name="id">Unique id of secret.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Secret?> GetSecretById(string id, CancellationToken cancellationToken = default);
        /// <summary>
        /// Retrieve first secret matching name, ordered by secret type.
        /// </summary>
        /// <param name="name">Name of secret.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Secret?> GetSecretByName(string name, CancellationToken cancellationToken = default);
        /// <summary>
        /// Retrieve list of all secrets in store.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IEnumerable<Secret>> GetSecrets(CancellationToken cancellationToken = default);
        /// <summary>
        /// Retrieve list of all secrets by type in store.
        /// </summary>
        /// <param name="type">Type of secrets.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IEnumerable<Secret>> GetSecrets(string type, bool decrypt = true, CancellationToken cancellationToken = default);
        /// <summary>
        /// Get view models of all secrets
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IEnumerable<Secret>> GetSecretViewModels(CancellationToken cancellationToken = default);
        /// <summary>
        /// Add or update secret if it exists
        /// </summary>
        /// <param name="secret">Secret to be saved</param>
        /// <param name="restoreHiddenProperties">In case of saving from frontend, properties will be be hidden and must be restored</param>
        /// <returns>The saved model, which may differ from the input</returns>
        Task<Secret> AddOrUpdateSecret(Secret secret, bool restoreHiddenProperties = false, CancellationToken cancellationToken = default);
    }
}