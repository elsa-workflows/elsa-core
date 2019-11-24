using System.Collections.Generic;
using Elsa.Server.GraphQL.Services;
using GraphQL.Types;

namespace Elsa.Server.GraphQL
{
    public class ElsaMutation : ObjectGraphType
    {
        public ElsaMutation(IEnumerable<IMutationProvider> providers)
        {
            foreach (var provider in providers) 
                provider.Setup(this);
        }
    }
}