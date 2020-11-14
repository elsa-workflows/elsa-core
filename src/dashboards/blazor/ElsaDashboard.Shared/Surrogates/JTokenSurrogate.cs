using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.Serialization.JsonNet;
using ProtoBuf;

namespace ElsaDashboard.Shared.Surrogates
{
    /// <summary>
    /// Surrogate for <see cref="JToken"/>.
    /// </summary>
    [ProtoContract(IgnoreListHandling = true)]
    public class JTokenSurrogate
    {
        private static readonly JsonSerializerSettings SerializerSettings;

        /// <summary>
        /// Default constructor.
        /// </summary>
        static JTokenSurrogate()
        {
            SerializerSettings = new JsonSerializerSettings().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        }

        /// <summary>
        /// Constructor overload. 
        /// </summary>
        public JTokenSurrogate(JToken value) => Value = JsonConvert.SerializeObject(value, SerializerSettings);

        /// <summary>
        /// Stores the Google Protobuf Any value for serialization.
        /// </summary>
        public string Value { get; set; } = default!;

        /// <summary>
        /// Converts the surrogate to a <see cref="JToken"/>.
        /// </summary>
        public static implicit operator JToken(JTokenSurrogate surrogate) => JsonConvert.DeserializeObject<JToken>(surrogate.Value, SerializerSettings);

        /// <summary>
        ///  Converts the <see cref="JToken"/> to a surrogate.
        /// </summary>
        public static implicit operator JTokenSurrogate(JToken source) => new(source);
    }
}