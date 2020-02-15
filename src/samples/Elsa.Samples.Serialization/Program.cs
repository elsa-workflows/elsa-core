using System;
using System.Collections.Generic;
using Elsa.Activities.Console;
using Elsa.Expressions;
using Elsa.Models;
using Elsa.Serialization;
using Elsa.Serialization.Formatters;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.Serialization
{
    class Program
    {
        static void Main(string[] args)
        {
            var services = new ServiceCollection()
                .AddElsa()
                .AddConsoleActivities()
                .BuildServiceProvider();
            var activityResolver = services.GetRequiredService<IActivityResolver>();
            var writeLine = activityResolver.ResolveActivity<WriteLine>().WithText(new LiteralExpression<string>("Foo"));
            var workflowDefinition = new WorkflowDefinitionVersion
            {
                Activities = new List<ActivityDefinition> { ActivityDefinition.FromActivity(writeLine) }
            };

            var serializer = services.GetRequiredService<IWorkflowSerializer>();
            var json = serializer.Serialize(workflowDefinition, JsonTokenFormatter.FormatName);
            
            Console.WriteLine(json);
            
            var workflowDefinition2 = serializer.Deserialize<WorkflowDefinitionVersion>(json, JsonTokenFormatter.FormatName);
            
        }
    }
}