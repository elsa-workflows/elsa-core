using Elsa.Secrets.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elsa.Activities.Sql.Services
{
    public interface ISecretsProvider
    {
        Task<ICollection<string>> GetSecrets(string type);
    }
}