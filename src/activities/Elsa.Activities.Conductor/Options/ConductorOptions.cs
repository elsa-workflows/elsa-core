using System;
using System.Collections.Generic;
using Elsa.Activities.Conductor.Models;
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
        public Uri CommandsHookUrl { get; set; } = default!;
        
        /// <summary>
        /// The URL to post tasks to.
        /// </summary>
        public Uri TasksHookUrl { get; set; } = default!;

        /// <summary>
        /// The serializer to use when serializing a command before sending to the application.
        /// </summary>
        public JsonSerializerSettings SerializerSettings { get; set; }

        /// <summary>
        /// A collection of commands that can be sent to the application.
        /// </summary>
        public ICollection<CommandDefinition> Commands { get; set; } = new List<CommandDefinition>();
        
        /// <summary>
        /// A collection of events that can be received from the application.
        /// </summary>
        public ICollection<EventDefinition> Events { get; set; } = new List<EventDefinition>();
        
        /// <summary>
        /// A collection of tasks that can be sent to the application.
        /// </summary>
        public ICollection<TaskDefinition> Tasks { get; set; } = new List<TaskDefinition>();
    }
}