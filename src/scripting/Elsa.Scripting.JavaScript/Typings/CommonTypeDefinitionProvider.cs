using System;
using System.Collections.Generic;
using System.Globalization;
using Elsa.Models;
using Elsa.Scripting.JavaScript.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;
using NodaTime;

namespace Elsa.Scripting.JavaScript.Typings
{
    public class CommonTypeDefinitionProvider : TypeDefinitionProvider
    {
        private static readonly IDictionary<Type, string> TypeMap = new Dictionary<Type, string>
        {
            [typeof(short)] = "number",
            [typeof(ushort)] = "number",
            [typeof(int)] = "number",
            [typeof(uint)] = "number",
            [typeof(long)] = "number",
            [typeof(ulong)] = "number",
            [typeof(float)] = "number",
            [typeof(double)] = "number",
            [typeof(decimal)] = "number",
            [typeof(string)] = "string",
            [typeof(char)] = "string",
            [typeof(bool)] = "boolean",
            [typeof(DateTime)] = "Date",
            [typeof(DateTimeOffset)] = "Date",
            [typeof(TimeSpan)] = "string",
            [typeof(TimeSpan)] = "string",
            [typeof(PathString)] = "string"
        };

        public override bool SupportsType(TypeDefinitionContext context, Type type) => TypeMap.ContainsKey(type);
        public override string GetTypeDefinition(TypeDefinitionContext context, Type type) => TypeMap[type];
        
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