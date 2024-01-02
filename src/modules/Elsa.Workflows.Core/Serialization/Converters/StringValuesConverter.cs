using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Elsa.Workflows.Serialization.Converters
{
    /// <summary>
    /// Serializes <see cref="StringValues"/> using the converter for <scc cref="T:string[]" /> arrays.
    /// </summary>
    public class StringValuesConverter : JsonConverter<StringValues>
    {
        /// <inheritdoc/>
        public override StringValues Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var stringArrayConverter = (JsonConverter<string[]>)options.GetConverter(typeof(string[]));

            var stringArray = stringArrayConverter.Read(ref reader, typeof(string[]), options);

            return new StringValues(stringArray);
        }
        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, StringValues value, JsonSerializerOptions options)
        {
            var stringArrayConverter = (JsonConverter<string[]>)options.GetConverter(typeof(string[]));

            var stringArray = value.ToArray();

            stringArrayConverter.Write(writer, stringArray!, options);
        }
    }
}
