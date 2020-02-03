using System.Collections.Generic;
using System.Linq;
using Elsa.Metadata;
using Elsa.Services;

namespace Elsa.Server.GraphQL2.Queries
{
    public class Query
    {
        private readonly IActivityResolver activityResolver;

        public Query(IActivityResolver activityResolver)
        {
            this.activityResolver = activityResolver;
        }

        public IEnumerable<ActivityDescriptor> GetActivityDescriptors() => 
            activityResolver.GetActivityTypes().Select(ActivityDescriber.Describe).ToList();
        
        public ActivityDescriptor? GetActivityDescriptor(string typeName)
        {
            var type = activityResolver.GetActivityType(typeName);

            return type == null ? default : ActivityDescriber.Describe(type);
        }
    }
}