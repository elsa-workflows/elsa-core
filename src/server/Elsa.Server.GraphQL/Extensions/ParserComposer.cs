using System;
using System.Collections.Generic;
using System.Linq;

namespace Elsa.Server.GraphQL.Extensions
{
    public static class ParserComposer
    {
        public static object FirstNonThrowing<T>(IEnumerable<Func<string, T>> parsers, string value)
        {
            if (!parsers.Any())
                return null;

            foreach (var nextParser in parsers)
            {
                try
                {
                    return nextParser(value);
                }
                catch (Exception) { continue; }
            }

            return null;
        }
    }
}