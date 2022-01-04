using Elsa.Contracts;

namespace Elsa.Services;

public class IdentityGenerator : IIdentityGenerator
{
    public string GenerateId() => Guid.NewGuid().ToString("N");
}