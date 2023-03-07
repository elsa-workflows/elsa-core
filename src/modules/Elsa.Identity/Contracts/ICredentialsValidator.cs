using Elsa.Identity.Entities;

namespace Elsa.Identity.Contracts;

public interface ICredentialsValidator
{
    ValueTask<User?> ValidateAsync(string username, string password, CancellationToken cancellationToken = default);
}