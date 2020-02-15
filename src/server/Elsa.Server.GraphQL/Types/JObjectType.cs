using System;
using System.Collections.Generic;
using Elsa.Models;
using Elsa.Serialization;
using Elsa.Serialization.Formatters;
using HotChocolate.Language;
using HotChocolate.Types;
using Humanizer;
using Newtonsoft.Json.Linq;

namespace Elsa.Server.GraphQL.Types
{
    public class JObjectType : ScalarType
    {
        public JObjectType() : base("JObjectType")
        {
        }

        public override Type ClrType => typeof(JObject);

        public override bool IsInstanceOfType(IValueNode literal)
        {
            return literal == null || literal is ObjectValueNode || literal is NullValueNode;
        }

        public override object ParseLiteral(IValueNode literal)
        {
            return new JObject();
        }

        public override IValueNode ParseValue(object value)
        {
            if (value is null)
                return NullValueNode.Default;

            return new ObjectValueNode();
        }

        public override object Serialize(object value)
        {
            if (value == null)
                return null;
            
            var jObject = (JObject)value;
            var dictionary = new Dictionary<string, object>();

            foreach (var variable in jObject) 
                dictionary[variable.Key.Camelize()] = variable.Value;

            return dictionary;
        }

        public override bool TryDeserialize(object serialized, out object value)
        {
            value = null;
            return false;
        }
    }
}