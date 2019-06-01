using System;
using Elsa.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Elsa.Extensions
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
            var activityObject = token == null
                ? Activator.CreateInstance(descriptor.ActivityModelType)
                : token.ToObject(descriptor.ActivityModelType, JsonSerializer.Create(new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                }));

            var activity = (IActivity) activityObject;

            activity.Descriptor = descriptor;
            return activity;
        }
    }
}