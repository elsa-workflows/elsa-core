using System;
using System.Collections.Generic;
using Elsa.Scripting.JavaScript.Services;

namespace Elsa.Scripting.JavaScript.Typings
{
    public class WorkflowVariablesTypeDefinitionProvider : TypeDefinitionProvider
    {
        public override IEnumerable<Type> CollectTypes(TypeDefinitionContext context)
        {
            var workflowDefinition = context.WorkflowDefinition;

            if (workflowDefinition == null) 
                yield break;
            
            foreach (var variable in workflowDefinition.Variables!.Data.Values)
                yield return variable!.GetType();
        }
    }
}