using System;
using System.Collections.Generic;
using Elsa.Scripting.JavaScript.Services;

namespace Elsa.Scripting.JavaScript.Typings
{
    public class WorkflowContextTypeDefinitionProvider : TypeDefinitionProvider
    {
        public override IEnumerable<Type> CollectTypes(TypeDefinitionContext context)
        {
            var workflowDefinition = context.WorkflowDefinition;
            var contextType = workflowDefinition?.ContextOptions?.ContextType;

            if (contextType != null)
                yield return contextType;
        }
    }
}