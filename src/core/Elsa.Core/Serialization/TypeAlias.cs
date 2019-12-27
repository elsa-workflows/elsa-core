using System;

namespace Elsa.Serialization
{
    public class TypeAlias : ITypeAlias
    {
        public TypeAlias(Type type, string alias)
        {
            Type = type;
            Alias = alias;
        }
        
        public Type Type { get; }
        public string Alias { get; }
    }

    public class TypeAlias<T> : TypeAlias
    {
        public TypeAlias(string alias) : base(typeof(T), alias)
        {
        }
    }
}