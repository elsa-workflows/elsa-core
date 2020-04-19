using System;
using System.Collections.Generic;
using Elsa.Models;
using Elsa.Serialization;
using HotChocolate.Language;
using HotChocolate.Types;

namespace Elsa.Server.GraphQL.Types
{
    public class VariablesType : ScalarType
    {
        private readonly ITokenSerializer serializer;

        public VariablesType(ITokenSerializer serializer) : base("Variables")
        {
            this.serializer = serializer;
        }

        public override Type ClrType => typeof(Variables);

        public override bool IsInstanceOfType(IValueNode literal)
        {
            return literal == null || literal is ObjectValueNode || literal is NullValueNode;
        }

        public override object ParseLiteral(IValueNode literal)
        {
            return new Variables();
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
            
            var variables = (Variables)value;
            var dictionary = new Dictionary<string, object>();

            foreach (var variable in variables)
            {
                var v = serializer.Serialize(variable.Value);
                dictionary[variable.Key] = v;
            }
            
            return dictionary;
        }

        public override bool TryDeserialize(object serialized, out object value)
        {
            value = null;
            return false;
        }
    }
}