using System.Text.Json;
using Elsa.Common;
using Elsa.Common.Models;
using Elsa.Common.Services;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Exceptions;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Management.Stores;
using NSubstitute;

namespace Elsa.Workflows.Management.UnitTests.Services;

public class WorkflowDefinitionPublisherDraftTests
{
    [Fact]
    public async Task SaveDraftRefusesAConcurrentProtectionChange()
    {
        var store = new MemoryWorkflowDefinitionStore(new MemoryStore<WorkflowDefinition>());
        var persisted = new WorkflowDefinition
        {
            Id = "version",
            DefinitionId = "definition",
            Version = 1,
            IsLatest = true,
            Name = "Original"
        };
        await store.SaveAsync(persisted);

        var mediator = Substitute.For<IMediator>();
        mediator.SendAsync(Arg.Any<WorkflowDefinitionDraftSaving>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                var protectedDefinition = persisted.ShallowClone();
                protectedDefinition.IsReadonly = true;
                await store.SaveAsync(protectedDefinition);
            });
        var serializer = Substitute.For<IActivitySerializer>();
        serializer.Serialize(Arg.Any<object>()).Returns(call => JsonSerializer.Serialize(call.Arg<object>()));
        var publisher = new WorkflowDefinitionPublisher(Substitute.For<IWorkflowDefinitionService>(), store,
            Substitute.For<IWorkflowValidator>(), Substitute.For<IIdentityGenerator>(), serializer, mediator,
            Substitute.For<ISystemClock>(), Microsoft.Extensions.Options.Options.Create(new ManagementOptions()));

        var draft = await publisher.GetDraftAsync("definition", VersionOptions.Latest);
        Assert.NotNull(draft);
        draft.Name = "Prepared update";
        await Assert.ThrowsAsync<WorkflowDefinitionEditConflictException>(() => publisher.SaveDraftAsync(draft));

        var stored = await store.FindAsync(new WorkflowDefinitionFilter { Id = "version" });
        Assert.NotNull(stored);
        Assert.True(stored.IsReadonly);
        Assert.Equal("Original", stored.Name);
    }
}
