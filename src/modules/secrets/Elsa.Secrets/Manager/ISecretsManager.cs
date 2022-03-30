using Elsa.Secrets.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Secrets.Manager
{
    public interface ISecretsManager
    {
        Task<IEnumerable<Secret>> GetSecrets(CancellationToken cancellationToken = default);
        Task<IEnumerable<Secret>> GetSecrets(string type, CancellationToken cancellationToken = default);
    }
}