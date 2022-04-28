using Elsa.Services;

namespace Elsa.Implementations;

public class RandomIdentityGenerator : IIdentityGenerator
{
    public string GenerateId() => Guid.NewGuid().ToString("N");
}