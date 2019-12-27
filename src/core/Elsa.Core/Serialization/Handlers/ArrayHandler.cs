using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Serialization.Handlers
{
    public class ArrayHandler : IValueHandler
    {
        private const string ItemTypeFieldName = "itemType";
        private const string ItemsFieldName = "items";
        
        public int Priority => 0;
        public bool CanSerialize(object value, JToken token, Type type) => token.Type == JTokenType.Array;
        public bool CanDeserialize(JToken token, Type type) => token.Type == JTokenType.Object && token[ItemTypeFieldName] != null;
        
        public void Serialize(JsonWriter writer, JsonSerializer serializer, Type type, JToken token, object? value)
        {
            var elementType = type.GetElementType();
            var arrayToken = new JObject
            {
                [ItemTypeFieldName] = GetAssemblyQualifiedTypeName(elementType), 
                [ItemsFieldName] = token
            };

            arrayToken.WriteTo(writer, serializer.Converters.ToArray());
        }
        
        public object Deserialize(JsonSerializer serializer, Type type, JToken token)
        {
            var itemTypeName = token[ItemTypeFieldName]?.Value<string>();
            
            if(itemTypeName == null)
                throw new InvalidOperationException();
            
            var itemType = Type.GetType(itemTypeName);
            
            if(itemType == null)
                throw new InvalidOperationException();
            
            var itemsToken = (JArray)token[ItemsFieldName];
            var items = itemsToken.Select(itemToken => itemToken.ToObject(itemType, serializer)).ToArray();
            var array = Array.CreateInstance(itemType, items.Length);

            for (var i = 0; i < items.Length; i++) 
                array.SetValue(items[i], i);

            return array;
        }
        
        private static string GetAssemblyQualifiedTypeName(Type type)
        {
            var typeName = type.FullName;
            var assemblyName = type.Assembly.GetName().Name;

            return $"{typeName}, {assemblyName}";
        }
    }
}