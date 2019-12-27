using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Elsa.Serialization
{
    public class TypeMap : ITypeMap
    {
        private readonly IDictionary<string, Type> byAlias;
        private readonly IDictionary<Type, string> byType;

        public TypeMap(IEnumerable<ITypeAlias> aliases)
        {
            var list = aliases.ToArray();
            byAlias = list.ToDictionary(x => x.Alias, x => x.Type);
            byType = list.ToDictionary(x => x.Type, x => x.Alias);
        }
        
        public Type GetType(string alias)
        {
            var parsedAlias = ParseAlias(alias).ToList();
            return MakeGenericType(parsedAlias);
        }

        public string GetAlias(Type type)
        {
            if (type.IsConstructedGenericType)
            {
                var alias = GetAlias(type.GetGenericTypeDefinition());
                var genericAliases = string.Join(":", type.GenericTypeArguments.Select(GetAlias));
                return $"{alias}[{genericAliases}]";
            }
            else
            {
                return byType.TryGetValue(type, out var alias) ? alias : type.AssemblyQualifiedName;    
            }
        }
        
        private Type ResolveAlias(string alias) => byAlias.TryGetValue(alias, out var type) ? type : Type.GetType(alias);

        public Type MakeGenericType(IList<IList<Type>> stack)
        {
            var currentSet = default(IList<Type>);
	
            for (var i = 0; i < stack.Count; i++)
            {
                var set = stack[i];
		
                foreach (var type in set)
                {
                    if(currentSet != null)
                    {
                        stack[i] = currentSet = set = new[] { type.MakeGenericType(currentSet.ToArray())};
                    }
                }
		
                currentSet = set;
            }
	
            return currentSet.First();
        }

        private Stack<IList<Type>> ParseAlias(string alias)
        {
            var aliasPart = new StringBuilder();
            var stack = new Stack<char>(alias.Reverse());
            var typeList = new List<Type>();
            var typeStack = new Stack<IList<Type>>();
	
            while(stack.Any())
            {
                var character = stack.Pop();
                if(character == '[')
                {
                    typeList.Add(ResolveAlias(aliasPart.ToString()));
                    typeStack.Push(typeList);
                    typeList = new List<Type>();
                    aliasPart = new StringBuilder();
                }
                else if(character == ']')
                {
                    typeList.Add(ResolveAlias(aliasPart.ToString()));
                    typeStack.Push(typeList);
                    return typeStack;
                }
                else if(character == ':')
                {
                    typeList.Add(ResolveAlias(aliasPart.ToString()));
                    aliasPart = new StringBuilder();
                }
                else
                {
                    aliasPart.Append(character);
                }
            }
	
            typeList.Add(ResolveAlias(aliasPart.ToString()));
            typeStack.Push(typeList);
            
            return typeStack;
        }
    }
}