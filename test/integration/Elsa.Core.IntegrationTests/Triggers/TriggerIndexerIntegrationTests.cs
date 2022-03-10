using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
using Elsa.Activities.Signaling;
using Elsa.Builders;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.Triggers;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.Core.IntegrationTests.Triggers
{
    public class TriggerIndexerIntegrationTests
    {
        [Fact(DisplayName = "The IndexTriggersAsync method should return a trigger for a blocking start activity")]
        public async Task IndexTriggersAsyncShouldIncludeBlockingStartActivities()
        {
            var allTriggers = await IndexThenGetTriggersAsync(nameof(WorkflowWithBlockingStartActivity));
            Assert.True(allTriggers.Any(x => x.ActivityType == nameof(SignalReceived)),
                "A trigger exists for the expected blocking activity");
        }

        [Fact(DisplayName = "The IndexTriggersAsync method should not return a trigger for a non-blocking start activity")]
        public async Task IndexTriggersAsyncShouldNotIncludeNonBlockingStartActivities()
        {
            var allTriggers = await IndexThenGetTriggersAsync(nameof(WorkflowWithNonBlockingStartActivity));
            Assert.False(allTriggers.Any(),
                "No triggers exist for the workflow which starts with a non-blocking activity");
        }

        [Fact(DisplayName = "The IndexTriggersAsync method should return a trigger for a blocking start activity within a composite activity",
            Skip = "This test is the repro case for #738, which is not yet implemented")]
        public async Task IndexTriggersAsyncShouldIncludeBlockingCompositeStartActivities()
        {
            var allTriggers = await IndexThenGetTriggersAsync("WorkflowWithBlockingCompositeStartActivity");
            Assert.True(allTriggers.Any(),
                "A trigger exists for the expected blocking composite activity");
        }

        [Fact(DisplayName = "The IndexTriggersAsync method should not return a trigger for a non-blocking start activity within a composite activity")]
        public async Task IndexTriggersAsyncShouldNotIncludeNonBlockingCompositeStartActivities()
        {
            var allTriggers = await IndexThenGetTriggersAsync("WorkflowWithNonBlockingCompositeStartActivity");
            Assert.False(allTriggers.Any(),
                "No triggers exist for the workflow which starts with a non-blocking composite activity");
        }

        private async Task<IEnumerable<Trigger>> IndexThenGetTriggersAsync(string workflowDefinitionId)
        {
            var serviceProvider = await GetServiceProvider();
            var sut = serviceProvider.GetRequiredService<ITriggerIndexer>();
            var workflowBlueprint = await serviceProvider.GetRequiredService<IWorkflowRegistry>().FindAsync(workflowDefinitionId, VersionOptions.Published);
            await sut.IndexTriggersAsync();
            var triggerStore = serviceProvider.GetRequiredService<ITriggerStore>();
            return await triggerStore.FindManyAsync(new WorkflowDefinitionIdSpecification(workflowDefinitionId));
        }

        private async Task<IServiceProvider> GetServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddElsa(elsa =>
            {
                elsa
                    .AddWorkflow<WorkflowWithBlockingStartActivity>()
                    .AddWorkflow<WorkflowWithNonBlockingStartActivity>()
                    .AddActivity<SignalReceived>()
                    .AddActivity<SetVariable>()
                    ;
            });

            var serviceProvider = services.BuildServiceProvider();

            var definitionStore = serviceProvider.GetRequiredService<IWorkflowDefinitionStore>();
            await definitionStore.AddAsync(new WorkflowDefinition
            {
                Id = "1",
                DefinitionId = "WorkflowWithNonBlockingCompositeStartActivity",
                Name = "WorkflowWithNonBlockingCompositeStartActivity",
                Version = 1,
                IsPublished = true,
                IsLatest = true,
                PersistenceBehavior = WorkflowPersistenceBehavior.Suspended,
                Activities = new[]
                {
                    GetNonBlockingCompositeActivityDefinition("nonBlockingComposite1"),
                    GetNonBlockingActivityDefinition("nonBlocking1"),
                },
                Connections = new[]
                {
                    new ConnectionDefinition("nonBlockingComposite1", "nonBlocking1", OutcomeNames.Done),
                }
            });
            await definitionStore.AddAsync(new WorkflowDefinition
            {
                Id = "2",
                DefinitionId = "WorkflowWithBlockingCompositeStartActivity",
                Name = "WorkflowWithBlockingCompositeStartActivity",
                Version = 1,
                IsPublished = true,
                IsLatest = true,
                PersistenceBehavior = WorkflowPersistenceBehavior.Suspended,
                Activities = new[]
                {
                    GetBlockingCompositeActivityDefinition("blockingComposite1"),
                    GetNonBlockingActivityDefinition("nonBlocking2"),
                },
                Connections = new[]
                {
                    new ConnectionDefinition("blockingComposite1", "nonBlocking2", OutcomeNames.Done),
                }
            });

            return serviceProvider;
        }

        class WorkflowWithBlockingStartActivity : IWorkflow
        {
            public void Build(IWorkflowBuilder builder)
            {
                builder
                    .StartWith<SignalReceived>(a => a.Set(x => x.Signal, "MySignal").Set(x => x.Id, "SignalReceived1"))
                    .Then<Finish>(a => a.Set(x => x.Id, "Finish1"));
            }
        }

        class WorkflowWithNonBlockingStartActivity : IWorkflow
        {
            public void Build(IWorkflowBuilder builder)
            {
                builder
                    .StartWith<SetVariable>(t => t.Set(x => x.VariableName, "Unused").Set(x => x.Value, "Unused").Set(x => x.Id, "SetVariable1"))
                    .Then<Finish>(a => a.Set(x => x.Id, "Finish2"));
            }
        }

        ActivityDefinition GetBlockingActivityDefinition(string id)
        {
            return new()
            {
                ActivityId = id,
                Type = nameof(SignalReceived),
                Properties = new[]
                {
                    ActivityDefinitionProperty.Literal(nameof(SignalReceived.Signal), "MySignal"),
                }
            };
        }

        ActivityDefinition GetNonBlockingActivityDefinition(string id)
        {
            return new()
            {
                ActivityId = id,
                Type = nameof(SetVariable),
                Properties = new[]
                {
                    ActivityDefinitionProperty.Literal(nameof(SetVariable.VariableName), "Unused"),
                    ActivityDefinitionProperty.Literal(nameof(SetVariable.Value), "Unused"),
                }
            };
        }

        ActivityDefinition GetBlockingCompositeActivityDefinition(string id)
        {
            return new CompositeActivityDefinition
            {
                ActivityId = id,
                Activities = new[]
                {
                    GetBlockingActivityDefinition("SignalReceived2"),
                    GetNonBlockingActivityDefinition("SetVariable2"),
                },
                Connections = new[]
                {
                    new ConnectionDefinition("UserTask2", "SetVariable2", OutcomeNames.Done),
                }
            };
        }

        ActivityDefinition GetNonBlockingCompositeActivityDefinition(string id)
        {
            return new CompositeActivityDefinition
            {
                ActivityId = id,
                Activities = new[]
                {
                    GetNonBlockingActivityDefinition("SetVariable3"),
                    GetNonBlockingActivityDefinition("SetVariable4"),
                },
                Connections = new[]
                {
                    new ConnectionDefinition("SetVariable3", "SetVariable4", OutcomeNames.Done),
                }
            };
        }
    }
}