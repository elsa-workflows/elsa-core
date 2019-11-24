using System.Collections.Generic;
using Elsa.Server.GraphQL.Services;
using GraphQL.Types;

namespace Elsa.Server.GraphQL
{
    public class ElsaQuery : ObjectGraphType
    {
        public ElsaQuery(IEnumerable<IQueryProvider> providers)
        {
            foreach (var provider in providers) 
                provider.Setup(this);
        }
    }
}