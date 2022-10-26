using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Activities.Http.Models;
using Elsa.Scripting.JavaScript.Services;

namespace Elsa.Activities.Http.Scripting.JavaScript
{
    public class HttpTypeDefinitionProvider : TypeDefinitionProvider
    {
        public override IEnumerable<Type> CollectTypes(TypeDefinitionContext context)
        {
            var workflowDefinition = context.WorkflowDefinition;

            if (workflowDefinition != null)
            {
                // For each HTTP Endpoint activity, determine its TargetType. If configured, register it.
                var httpEndpointActivities = workflowDefinition.Activities.Where(x => x.Type == nameof(HttpEndpoint)).ToList();

                foreach (var activityDefinition in httpEndpointActivities)
                {
                    var property = activityDefinition.Properties.FirstOrDefault(x => x.Name == nameof(HttpEndpoint.TargetType));

                    if (property == null)
                        continue;

                    var targetTypeName = property.Expressions.Values.FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(targetTypeName))
                        continue;

                    var type = Type.GetType(targetTypeName);

                    if (type != null)
                        yield return type;
                }
            }

            yield return typeof(HttpRequestModel);
            yield return typeof(HttpResponseHeaders);
            yield return typeof(HttpResponseModel);
        }
    }
}
