using System;

namespace Elsa.Services
{
    public class IdGenerator : IIdGenerator
    {
        public string Generate() => Guid.NewGuid().ToString("N");
    }
}