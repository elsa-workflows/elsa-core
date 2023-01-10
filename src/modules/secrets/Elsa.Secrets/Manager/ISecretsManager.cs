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
        Task<IEnumerable<Secret>> GetSecrets(string type, CancellationToken cancellationToken = default);
    }
}