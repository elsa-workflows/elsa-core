using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Serialization.Handlers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Converters
{
    public class TypeNameHandlingConverter : JsonConverter
    {
        private static readonly IDictionary<Type, IValueHandler> ValueHandlers = new Dictionary<Type, IValueHandler>();

        public static void RegisterTypeHandler<T>() where T : IValueHandler
        {
            var handler = Activator.CreateInstance<T>();
            RegisterTypeHandler(handler);
        }
        
        public static void RegisterTypeHandler(IValueHandler handler)
        {
            ValueHandlers[handler.GetType()] = handler;
        }

        static TypeNameHandlingConverter()
        {
            RegisterTypeHandler<ObjectHandler>();
            RegisterTypeHandler<DateTimeHandler>();
            RegisterTypeHandler<InstantHandler>();
            RegisterTypeHandler<AnnualDateHandler>();
            RegisterTypeHandler<DurationHandler>();
            RegisterTypeHandler<LocalDateHandler>();
            RegisterTypeHandler<LocalDateTimeHandler>();
            RegisterTypeHandler<LocalTimeHandler>();
            RegisterTypeHandler<OffsetDateHandler>();
            RegisterTypeHandler<OffsetHandler>();
            RegisterTypeHandler<OffsetTimeHandler>();
            RegisterTypeHandler<YearMonthHandler>();
            RegisterTypeHandler<ZonedDateTimeHandler>();
        }
        
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var valueType = value.GetType();
            var token = JToken.FromObject(value);
            var handler = GetHandler(x => x.CanSerialize(token, valueType));

            handler.Serialize(writer, serializer, valueType, token);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.ReadFrom(reader);
            var handler = GetHandler(x => x.CanDeserialize(token, objectType));

            return handler.Deserialize(reader, serializer, objectType, token);
        }

        public override bool CanConvert(Type objectType) => true;

        private IValueHandler GetHandler(Func<IValueHandler, bool> predicate) => 
            ValueHandlers.Values.OrderByDescending(x => x.Priority).FirstOrDefault(predicate) ?? new DefaultValueHandler();
    }
}