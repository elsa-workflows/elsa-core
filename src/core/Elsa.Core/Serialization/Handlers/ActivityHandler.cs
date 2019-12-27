using System;
using System.Linq;
using System.Reflection;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Serialization.Handlers
{
    public class ActivityHandler : IValueHandler
    {
        private const string TypeFieldName = "type";
        private readonly IActivityResolver activityResolver;

        public ActivityHandler(IActivityResolver activityResolver)
        {
            this.activityResolver = activityResolver;
        }

        public int Priority => 0;
        public bool CanSerialize(object value, JToken token, Type type) => typeof(IActivity).IsAssignableFrom(type);
        public bool CanDeserialize(JToken token, Type type) => token["type"] != null && token["state"] != null && token["left"] != null;
        
        public object Deserialize(JsonSerializer serializer, Type type, JToken token)
        {
            var activityType = token[TypeFieldName]?.Value<string>();
            
            if(activityType == null)
                throw new InvalidOperationException();

            var activity = activityResolver.ResolveActivity(activityType);
            activity.Id = token["id"]?.Value<string>();
            activity.Name = token["name"]?.Value<string>();
            activity.DisplayName = token["displayName"]?.Value<string>();
            activity.State = token["state"]?.ToObject<Variables>(serializer);
            
            return activity;
        }

        public void Serialize(JsonWriter writer, JsonSerializer serializer, Type type, JToken token, object? value)
        {
            var activity = (IActivity)value;
            var activityDefinition = ActivityDefinition.FromActivity(activity);
            var activityToken = JToken.FromObject(activityDefinition, serializer);
            
            activityToken.WriteTo(writer, serializer.Converters.ToArray());
        }
    }
}