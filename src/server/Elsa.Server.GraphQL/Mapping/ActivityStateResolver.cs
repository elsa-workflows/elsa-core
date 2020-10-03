using AutoMapper;
using Elsa.Models;
using Elsa.Serialization;
using Elsa.Server.GraphQL.Types;
using Elsa.Services;

namespace Elsa.Server.GraphQL.Mapping
{
    public class ActivityStateResolver : IValueResolver<ActivityDefinitionInput, ActivityDefinitionRecord, Variables?>
    {
        private readonly ITokenSerializer serializer;
        private readonly IActivityResolver activityResolver;

        public ActivityStateResolver(ITokenSerializer serializer, IActivityResolver activityResolver)
        {
            this.serializer = serializer;
            this.activityResolver = activityResolver;
        }

        public Variables? Resolve(ActivityDefinitionInput source, ActivityDefinitionRecord destination, Variables? destMember, ResolutionContext context)
        {
            var json = source.State;
            var variables = json != null ? serializer.Deserialize<Variables>(json) : null;

            return variables;
        }
    }
}