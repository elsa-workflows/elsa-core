using System;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Implementations;

public class RandomIdentityGenerator : IIdentityGenerator
{
    public string GenerateId() => Guid.NewGuid().ToString("N");
}