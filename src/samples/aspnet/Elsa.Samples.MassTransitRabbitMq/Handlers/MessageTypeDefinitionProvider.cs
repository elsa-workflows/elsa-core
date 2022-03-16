using System;
using System.Collections.Generic;
using Elsa.Samples.MassTransitRabbitMq.Messages;
using Elsa.Scripting.JavaScript.Services;

namespace Elsa.Samples.MassTransitRabbitMq.Handlers
{
    /// <summary>
    /// Register custom types with the type definition provider (used for intellisense).
    /// </summary>
    public class MessageTypeDefinitionProvider : TypeDefinitionProvider
    {
        public override IEnumerable<Type> CollectTypes(TypeDefinitionContext context)
        {
            return new[] { typeof(FirstMessage), typeof(SecondMessage) };
        }
    }
}