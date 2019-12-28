using System;

namespace Elsa.Serialization
{
    public interface ITypeMap
    {
        Type GetType(string alias);
        string  GetAlias(Type type);
    }
}