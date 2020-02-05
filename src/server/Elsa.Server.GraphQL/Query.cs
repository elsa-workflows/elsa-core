using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Metadata;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;
using HotChocolate;

namespace Elsa.Server.GraphQL
{
    public class Query
    {
        public IEnumerable<ActivityDescriptor> GetActivityDescriptors(
            [Service] IActivityResolver activityResolver) =>
            activityResolver.GetActivityTypes().Select(ActivityDescriber.Describe).ToList();

        public ActivityDescriptor? GetActivityDescriptor([Service]IActivityResolver activityResolver, string typeName)
        {
            var type = activityResolver.GetActivityType(typeName);

            return type == null ? default : ActivityDescriber.Describe(type);
        }

        public async Task<IEnumerable<WorkflowDefinitionVersion>> GetWorkflowDefinitions([Service] IWorkflowDefinitionStore store, CancellationToken cancellationToken)
        {
            var x = (await store.ListAsync(VersionOptions.Latest, cancellationToken)).ToList();

            foreach (var workflowDefinitionVersion in x)
            {
                workflowDefinitionVersion.Variables = new Variables(new Dictionary<string, object>
                {
                    ["Foo"] = "Bar"
                });
            }
            
            return x;
        }
    }
}