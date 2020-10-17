using System.Linq;
using System.Reflection;
using Elsa.Expressions;
using Elsa.Serialization;
using Newtonsoft.Json.Linq;
using Rebus.Extensions;

namespace Elsa.Metadata.Handlers
{
    public class WorkflowExpressionOptionsProvider : IActivityPropertyOptionsProvider
    {
        public bool SupportsProperty(PropertyInfo property) => typeof(IWorkflowExpression).IsAssignableFrom(property.PropertyType);

        public void SupplyOptions(PropertyInfo property, JObject options)
        {
            var returnType = property.PropertyType.GetGenericArguments().FirstOrDefault() ?? typeof(object);
            var typeName = returnType.GetSimpleAssemblyQualifiedName();

            options["ReturnType"] = JToken.FromObject(typeName);
        }
    }
}