using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Elsa.Activities.Workflows.Helper
{
    public static class HashHelper
    {
        public static string Hash(object entity)
        {
            var seen = new HashSet<object>();
            var properties = GetAllSimpleProperties(entity, seen);
            return string.Join("",properties.Select(p => Encoding.ASCII.GetBytes(p.ToString()).AsEnumerable()).Aggregate((ag, next) => ag.Concat(next)).ToArray());
        }

        private static IEnumerable<object> GetAllSimpleProperties(object entity, HashSet<object> seen)
        {
            var properties = entity.GetType().GetProperties();
            if (properties.Length == 0)
            {
                yield return entity;
            }
            foreach (var property in properties)
            {
                var t = property.PropertyType;
                if (t == typeof(int) || t ==  typeof(long) || t == typeof(string)) yield return property.GetValue(entity) ?? "null";
                else if (seen.Add(property)) // Handle cyclic references
                {
                    foreach (var simple in GetAllSimpleProperties(property, seen)) yield return simple;
                }
            }
        }

        
    }
}
