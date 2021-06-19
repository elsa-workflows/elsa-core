using Elsa.Metadata;
using System.Collections.Generic;
using System.Reflection;

namespace Elsa.Activities.Http.Providers.DefaultValues
{
    public class HttpEndpointDefaultMethodsProvider : IActivityPropertyDefaultValueProvider
    {
        public object GetDefaultValue(PropertyInfo property)
        {
            return new HashSet<string> { "GET" };
        }
    }
}
