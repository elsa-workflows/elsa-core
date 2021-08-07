using System;
using System.Collections.Generic;
using Elsa.Scripting.JavaScript.Services;

namespace Elsa.Activities.Email.Handlers
{
    public class EmailTypeDefinitionProvider : TypeDefinitionProvider
    {
        public override IEnumerable<Type> CollectTypes(TypeDefinitionContext context)
        {
            return new[] { typeof(EmailAttachment) };
        }
    }
}