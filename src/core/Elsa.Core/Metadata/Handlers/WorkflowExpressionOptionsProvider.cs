using System.Linq;
using System.Reflection;
using Elsa.Expressions;
using Elsa.Serialization;
using Newtonsoft.Json.Linq;

namespace Elsa.Metadata.Handlers
{
    public class WorkflowExpressionOptionsProvider : IActivityPropertyOptionsProvider
    {
        private readonly ITypeMap _typeMap;

        public WorkflowExpressionOptionsProvider(ITypeMap typeMap)
        {
            _typeMap = typeMap;
        }
        
        public bool SupportsProperty(PropertyInfo property) => typeof(IWorkflowExpression).IsAssignableFrom(property.PropertyType);

        public void SupplyOptions(PropertyInfo property, JObject options)
        {
            var returnType = property.PropertyType.GetGenericArguments().FirstOrDefault() ?? typeof(object);
            var alias = _typeMap.GetAlias(returnType);

            options["ReturnType"] = JToken.FromObject(alias);
        }
    }
}