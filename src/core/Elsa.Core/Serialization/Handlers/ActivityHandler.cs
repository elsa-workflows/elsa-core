// using System;
// using System.Linq;
// using Elsa.Models;
// using Elsa.Services;
// using Newtonsoft.Json;
// using Newtonsoft.Json.Linq;
//
// namespace Elsa.Serialization.Handlers
// {
//     public class ActivityHandler : IValueHandler
//     {
//         private const string TypeFieldName = "activityType";
//         private readonly IActivityResolver activityResolver;
//
//         public ActivityHandler(IActivityResolver activityResolver)
//         {
//             this.activityResolver = activityResolver;
//         }
//
//         public int Priority => 0;
//         public bool CanSerialize(JToken token, Type type, object value) => typeof(IActivity).IsAssignableFrom(type);
//         public bool CanDeserialize(JToken token) => token.Type == JTokenType.Object && token[TypeFieldName] != null;
//
//         public object Deserialize(JsonSerializer serializer, JToken token)
//         {
//             var activityType = token[TypeFieldName]?.Value<string>();
//
//             if (activityType == null)
//                 throw new InvalidOperationException();
//
//             var activity = activityResolver.ResolveActivity(activityType);
//             activity.Id = token["id"]?.Value<string>();
//             activity.Name = token["name"]?.Value<string>();
//             activity.DisplayName = token["displayName"]?.Value<string>();
//             activity.Description = token["description"]?.Value<string>();
//             activity.State = token["state"]?.ToObject<Variables>(serializer);
//             activity.Output = token["output"]?.ToObject<Variable>(serializer);
//
//             return activity;
//         }
//
//         public void Serialize(JsonWriter writer, JsonSerializer serializer, Type type, JToken token, object? value)
//         {
//             var activity = (IActivity)value;
//             var activityToken = JToken.FromObject(
//                 new
//                 {
//                     Id = activity.Id,
//                     ActivityType = activity.Type,
//                     State = activity.State,
//                     Name = activity.Name,
//                     DisplayName = activity.DisplayName,
//                     Description = activity.Description,
//                     Output = activity.Output
//                 }, serializer);
//
//             activityToken.WriteTo(writer, serializer.Converters.ToArray());
//         }
//     }
// }