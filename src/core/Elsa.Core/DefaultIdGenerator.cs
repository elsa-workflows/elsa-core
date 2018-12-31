using System;

namespace Elsa
{
    public class DefaultIdGenerator : IIdGenerator
    {
        public string Generate() => Guid.NewGuid().ToString("N");
    }
}