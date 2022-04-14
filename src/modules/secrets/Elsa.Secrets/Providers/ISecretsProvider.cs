using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Secrets.Providers
{
    public interface ISecretsProvider
    {
        Task<ICollection<string>> GetSecrets(string type, string separator);
    }
}
