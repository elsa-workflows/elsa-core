using System;
using Elsa.Serialization.Converters;
using Newtonsoft.Json;

namespace Elsa.Design
{
    /// <summary>
    /// Represents settings to be used by the designer to invoke an Elsa's API endpoint to retrieve list options at runtime.
    /// </summary>
    public class RuntimeSelectListProviderSettings
    {
        public RuntimeSelectListProviderSettings(Type providerType, object? context = default)
        {
            RuntimeSelectListProviderType = providerType;
            Context = context;
        }

        /// <summary>
        /// The type of the list items provider. 
        /// </summary>
        [JsonConverter(typeof(FullTypeJsonConverter))]
        public Type RuntimeSelectListProviderType { get; }

        /// <summary>
        /// Optionally provide an object containing useful information for the list select items provider to determine what items to provide.
        /// </summary>
        [JsonProperty(TypeNameHandling = TypeNameHandling.All)]
        public object? Context { get; }
    }
}