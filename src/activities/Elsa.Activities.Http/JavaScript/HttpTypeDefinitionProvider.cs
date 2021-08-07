using System;
using System.Collections.Generic;
using Elsa.Activities.Http.Models;
using Elsa.Scripting.JavaScript.Services;

namespace Elsa.Activities.Http.JavaScript
{
    public class HttpTypeDefinitionProvider : TypeDefinitionProvider
    {
        public override IEnumerable<Type> CollectTypes(TypeDefinitionContext context)
        {
            return new[] { typeof(HttpRequestModel), typeof(HttpResponseHeaders), typeof(HttpResponseModel) };
        }
    }
}