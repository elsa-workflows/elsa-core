using System;

namespace Elsa.Serialization
{
    public interface ITypeAlias
    {
        Type Type { get; }
        string Alias { get; }
    }
}