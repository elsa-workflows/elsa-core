using System;
using Flowsharp.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Flowsharp.Extensions
{
    public static class ActivityDescriptorExtensions
    {
        public static IActivity InstantiateActivity(this ActivityDescriptor descriptor, string json)
        {
            var token = json != null ? JToken.Parse(json) : default;
            return InstantiateActivity(descriptor, token);
        }
        
        public static IActivity InstantiateActivity(this ActivityDescriptor descriptor, JToken token = default)
        {
            var activity = token == null
                ? Activator.CreateInstance(descriptor.ActivityType)
                : token.ToObject(descriptor.ActivityType, JsonSerializer.Create(new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                }));

            return (IActivity) activity;
        }
    }
}