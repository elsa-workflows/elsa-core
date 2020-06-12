using Elsa.Activities.Console;
using Elsa.Expressions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;
using Elsa.Builders;

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Elsa.Samples.WorkflowDefinition
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var services = new ServiceCollection()
                               .AddElsa()
                               .AddLogging(log => log.AddConsole())
                               .AddConsoleActivities()
                               .AddSingleton(Console.In)
                               .BuildServiceProvider();

            var activityResolver = services.GetRequiredService<IActivityResolver>();
            var activity1 = activityResolver.ResolveActivity<WriteLine>()
                                            .WithId("activity-1")
                                            .WithText(new LiteralExpression<string>("Hello world!"));

            var activity2 = activityResolver.ResolveActivity<WriteLine>()
                                            .WithId("activity-2")
                                            .WithText(new LiteralExpression<string>("Goodbye cruel world...!"));

            //Create Workflow Definition
            var workflowDefinition = new WorkflowDefinitionVersion
            {
                DefinitionId = "definition-001",
                IsPublished = true,
                Activities = new List<ActivityDefinition>
                {
                    ActivityDefinition.FromActivity(activity1),
                    ActivityDefinition.FromActivity(activity2)
                },
                Connections = new[]
                {
                    new ConnectionDefinition("activity-1", "activity-2", OutcomeNames.Done),
                }
            };

            //Register it
            var workflowDefination = services.GetService<IWorkflowDefinitionStore>();
            await workflowDefination.AddAsync(workflowDefinition);

            //Run it
            var invoker = services.GetService<IWorkflowHost>();
            await invoker.RunWorkflowDefinitionAsync("definition-001", "activity-1");

            Console.ReadLine();
        }
    }
}