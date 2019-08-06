using System;
using System.Collections;
using Microsoft.AspNetCore.JsonPatch.Adapters;
using Microsoft.AspNetCore.JsonPatch.Internal;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Elsa.Dashboard.Microsoft
{
    public sealed class CustomAdapterFactory : IAdapterFactory
    {
        public IAdapter Create(object target, IContractResolver contractResolver)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (contractResolver == null)
                throw new ArgumentNullException(nameof(contractResolver));
            var jsonContract = contractResolver.ResolveContract(target.GetType());
            if (target is JObject)
                return new JObjectAdapter();
            if (target is IList)
                return new ListAdapter();
            if (jsonContract is JsonDictionaryContract dictionaryContract)
                return (IAdapter) Activator.CreateInstance(
                    typeof(DictionaryAdapter<,>).MakeGenericType(dictionaryContract.DictionaryKeyType, dictionaryContract.DictionaryValueType)
                );
            if (jsonContract is JsonDynamicContract)
                return new DynamicObjectAdapter();
            return new PocoAdapter();
        }
    }
}