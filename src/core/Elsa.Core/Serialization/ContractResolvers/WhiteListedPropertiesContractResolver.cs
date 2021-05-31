using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Elsa.Serialization.ContractResolvers
{
    public class WhiteListedPropertiesContractResolver : DefaultContractResolver
    {
        private readonly string[] _whiteList;
        public WhiteListedPropertiesContractResolver(params string[] whiteList) => _whiteList = whiteList;

        public WhiteListedPropertiesContractResolver(IEnumerable<PropertyInfo> properties) : this(properties.Select(x => x.Name).ToArray())
        {
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var props = base.CreateProperties(type, memberSerialization);
            props = props.Where(p => _whiteList.Contains(p.PropertyName)).ToList();
            return props;
        }
    }
}