using System;
using Flowsharp.Models;
using Newtonsoft.Json;

namespace Flowsharp.Extensions
{
    public static class ActivityDescriptorExtensions
    {
        public static IActivity InstantiateActivity(this ActivityDescriptor descriptor, string json = null)
        {
            var activity = json == null
                ? Activator.CreateInstance(descriptor.ActivityType)
                : JsonConvert.DeserializeObject(json, descriptor.ActivityType);

            return (IActivity) activity;
        }
    }
}