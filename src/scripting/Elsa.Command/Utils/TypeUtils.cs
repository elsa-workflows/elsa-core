using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.CommandExecuter.Utils;
public static class TypeUtils
{
    public static Type GetType(string typeName)
    {
        return AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetType(typeName)).FirstOrDefault(x => x != null);
    }

    public static void AddRangeOrOverwrite(IDictionary<string, object> destination, Dictionary<string, object> source)
    {
        if (source != default && source.Count() > 0)
        {
            foreach (var item in source)
            {
                if (destination.ContainsKey(item.Key.ToLower()))
                    destination[item.Key.ToLower()] = item.Value;
                else
                    destination.Add(item.Key.ToLower(), item.Value);
            }
        }
    }

    public static object ChangeType(object value, Type conversion)
    {
        var t = conversion;

        if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
        {
            if (value == null)
            {
                return null;
            }

            t = Nullable.GetUnderlyingType(t);
        }

        return Convert.ChangeType(value, t);
    }

}
