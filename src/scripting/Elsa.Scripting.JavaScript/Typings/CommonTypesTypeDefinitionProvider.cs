using System;
using System.Collections.Generic;
using System.Globalization;
using Elsa.Models;
using Elsa.Scripting.JavaScript.Services;
using Elsa.Services.Models;
using NodaTime;

namespace Elsa.Scripting.JavaScript.Typings
{
    public class CommonTypesTypeDefinitionProvider : TypeDefinitionProvider
    {
        public override IEnumerable<Type> CollectTypes(TypeDefinitionContext context) => new[]
        {
            typeof(Instant),
            typeof(Duration),
            typeof(Period),
            typeof(LocalDate),
            typeof(LocalTime),
            typeof(LocalDateTime),
            typeof(Guid),
            typeof(CultureInfo),
            typeof(ActivityExecutionContext),
            typeof(WorkflowExecutionContext),
            typeof(WorkflowInstance),
        };
    }
}