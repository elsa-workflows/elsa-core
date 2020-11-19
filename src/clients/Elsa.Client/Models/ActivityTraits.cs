using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace Elsa.Client.Models
{
    [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [Flags]
    public enum ActivityTraits
    {
        Action = 1,
        Trigger = 2,
        Job = 4
    }
}