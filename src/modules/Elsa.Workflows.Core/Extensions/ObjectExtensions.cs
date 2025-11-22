using System.Reflection;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ObjectExtensions
{
    extension(object source)
    {
        /// <summary>
        /// Converts an object into a dictionary.
        /// </summary>
        public IDictionary<string, object> ToDictionary(BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance) => source.ToDictionaryInternal(bindingAttr);

        /// <summary>
        /// Converts an object into a dictionary.
        /// </summary>
        public IReadOnlyDictionary<string, object> ToReadOnlyDictionary(BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance) => source.ToDictionaryInternal(bindingAttr);

        private Dictionary<string, object> ToDictionaryInternal(BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance) =>
            source.GetType().GetProperties(bindingAttr).ToDictionary
            (
                propInfo => propInfo.Name,
                propInfo => propInfo.GetValue(source, null)!
            );
    }
}