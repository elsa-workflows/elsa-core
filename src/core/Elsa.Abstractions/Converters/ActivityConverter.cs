// using System;
// using System.Linq;
// using Elsa.Models;
// using Elsa.Services;
// using Newtonsoft.Json;
// using Newtonsoft.Json.Linq;
//
// namespace Elsa.Converters
// {
//     public class ActivityConverter : JsonConverter<IActivity>
//     {
//         private readonly IActivityResolver activityResolver;
//
//         public ActivityConverter(IActivityResolver activityResolver)
//         {
//             this.activityResolver = activityResolver;
//         }
//         
//         public override bool CanRead => true;
//         public override bool CanWrite => true;
//         
//         public override void WriteJson(JsonWriter writer, IActivity value, JsonSerializer serializer)
//         {
//             var activityToken = JToken.FromObject(
//                 new
//                 {
//                     Id = value.Id,
//                     ActivityType = value.Type,
//                     State = value.State,
//                     Name = value.Name,
//                     DisplayName = value.DisplayName,
//                     Output = value.Output,
//                     Description = value.Description
//                 }, serializer);
//
//             activityToken.WriteTo(writer, serializer.Converters.ToArray());
//         }
//
//         public override IActivity ReadJson(JsonReader reader, Type objectType, IActivity existingValue, bool hasExistingValue, JsonSerializer serializer)
//         {
//             var token = JToken.ReadFrom(reader);
//             var activityType = token["activityType"]?.Value<string>();
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
//     }
// }