using System;
using Elsa.Services;

namespace Elsa.Core.Services
{
    public class IdGenerator : IIdGenerator
    {
        public string Generate() => Guid.NewGuid().ToString("N");
    }
}