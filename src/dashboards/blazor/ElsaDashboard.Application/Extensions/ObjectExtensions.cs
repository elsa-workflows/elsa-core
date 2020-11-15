using System.Collections.Generic;
using System.Linq;

namespace ElsaDashboard.Application.Extensions
{
    public static class ObjectExtensions
    {
        public static Dictionary<string, object?> ToDictionary(this object value) => value.GetType().GetProperties().ToDictionary(x => x.Name, x => x.GetValue(value, null));
    }
}