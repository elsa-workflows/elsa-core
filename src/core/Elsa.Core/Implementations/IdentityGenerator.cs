using Elsa.Services;

namespace Elsa.Implementations;

public class IdentityGenerator : IIdentityGenerator
{
    public string GenerateId() => Guid.NewGuid().ToString("N");
}