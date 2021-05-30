using System;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Activities.Conductor.Options
{
    public class ConductorOptions
    {
        public ConductorOptions()
        {
            SerializerSettings = new JsonSerializerSettings();
            SerializerSettings.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        }
        
        /// <summary>
        /// The URL to post commands to.
        /// </summary>
        public Uri ApplicationHookUrl { get; set; } = default!;

        public JsonSerializerSettings SerializerSettings { get; set; }
    }
}