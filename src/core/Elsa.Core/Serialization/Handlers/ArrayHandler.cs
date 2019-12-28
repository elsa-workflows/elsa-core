using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Serialization.Handlers
{
    public class ArrayHandler : IValueHandler
    {
        private readonly ITypeMap typeMap;
        private const string ElementTypeFieldName = "elementType";
        private const string ElementsFieldName = "elements";

        public ArrayHandler(ITypeMap typeMap) => this.typeMap = typeMap;

        public int Priority => 0;
        public bool CanSerialize(JToken token, Type type, object value) => token.Type == JTokenType.Array;
        public bool CanDeserialize(JToken token) => token.Type == JTokenType.Object && token[ElementTypeFieldName] != null;
        
        public void Serialize(JsonWriter writer, JsonSerializer serializer, Type type, JToken token, object? value)
        {
            var elementType = GetElementType(type);
            var arrayToken = new JObject
            {
                [ElementTypeFieldName] = typeMap.GetAlias(elementType), 
                [ElementsFieldName] = token
            };

            arrayToken.WriteTo(writer, serializer.Converters.ToArray());
        }
        
        public object Deserialize(JsonSerializer serializer, JToken token)
        {
            var elementTypeName = token[ElementTypeFieldName]?.Value<string>();
            
            if(elementTypeName == null)
                throw new InvalidOperationException();
            
            var elementType = typeMap.GetType(elementTypeName);
            
            if(elementType == null)
                throw new InvalidOperationException();
            
            var elementsToken = (JArray)token[ElementsFieldName];
            var elements = elementsToken.Select(itemToken => itemToken.ToObject(elementType, serializer)).ToArray();
            var array = Array.CreateInstance(elementType, elements.Length);

            for (var i = 0; i < elements.Length; i++) 
                array.SetValue(elements[i], i);

            return array;
        }

        private Type GetElementType(Type listType) => listType.IsArray ? listType.GetElementType() : listType.GetGenericArguments().First();
    }
}