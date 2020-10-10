using AutoMapper;
using Elsa.Models;
using Elsa.Serialization;
using Elsa.Server.GraphQL.Types;
using Elsa.Services;

namespace Elsa.Server.GraphQL.Mapping
{
    public class ActivityStateResolver : IValueResolver<ActivityDefinitionInput, ActivityDefinition, Variables?>
    {
        private readonly ITokenSerializer _serializer;
        private readonly IActivityResolver _activityResolver;

        public ActivityStateResolver(ITokenSerializer serializer, IActivityResolver activityResolver)
        {
            _serializer = serializer;
            _activityResolver = activityResolver;
        }

        public Variables? Resolve(ActivityDefinitionInput source, ActivityDefinition destination, Variables? destMember, ResolutionContext context)
        {
            var json = source.State;
            var variables = json != null ? _serializer.Deserialize<Variables>(json) : null;

            return variables;
        }
    }
}