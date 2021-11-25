using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Activities.Http.Models;
using Elsa.Scripting.JavaScript.Services;

namespace Elsa.Activities.Http.JavaScript
{
    public class HttpTypeDefinitionProvider : TypeDefinitionProvider
    {
        public override IEnumerable<Type> CollectTypes(TypeDefinitionContext context)
        {
            var workflowDefinition = context.WorkflowDefinition;

            if (workflowDefinition != null)
            {
                var httpEndpointActivities = workflowDefinition.Activities.Where(x => x.Type == nameof(HttpEndpoint)).ToList();

                foreach (var type in
                         from activityDefinition in httpEndpointActivities
                         select activityDefinition.Properties.First(x => x.Name == nameof(HttpEndpoint.TargetType)).Expressions.Values.First()
                         into targetTypeName
                         where !string.IsNullOrWhiteSpace(targetTypeName)
                         select Type.GetType(targetTypeName)
                         into type
                         where type != null
                         select type)
                {
                    yield return type;
                }
            }

            yield return typeof(HttpRequestModel);
            yield return typeof(HttpResponseHeaders);
            yield return typeof(HttpResponseModel);
        }
    }
}