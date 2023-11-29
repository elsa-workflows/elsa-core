namespace Elsa.Secrets.Encryption
{
    using System.Threading;
    using System.Threading.Tasks;
    using Elsa.Secrets.Models;

    public interface ISecretEncryptor
    {
        Task EncryptProperties(Secret secret, CancellationToken cancellationToken = default);
        Task DecryptPropertiesAsync(Secret secret, CancellationToken cancellationToken = default);
    }
}
