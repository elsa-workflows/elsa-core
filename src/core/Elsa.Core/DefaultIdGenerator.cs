using System;

namespace Elsa.Core
{
    public class DefaultIdGenerator : IIdGenerator
    {
        public string Generate() => Guid.NewGuid().ToString("N");
    }
}