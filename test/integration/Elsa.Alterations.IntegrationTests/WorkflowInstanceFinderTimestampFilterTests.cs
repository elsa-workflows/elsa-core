using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Models;
using Elsa.Alterations.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Enums;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Alterations.IntegrationTests;

public class WorkflowInstanceFinderTimestampFilterTests : IAsyncDisposable
{
    private readonly IServiceProvider _services;
    private readonly IWorkflowInstanceFinder _workflowInstanceFinder;
    private readonly IWorkflowInstanceStore _workflowInstanceStore;

    public WorkflowInstanceFinderTimestampFilterTests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
            .ConfigureElsa(elsa => elsa.UseAlterations())
            .Build();
        
        _workflowInstanceFinder = _services.GetRequiredService<IWorkflowInstanceFinder>();
        _workflowInstanceStore = _services.GetRequiredService<IWorkflowInstanceStore>();
    }

    public async ValueTask DisposeAsync()
    {
        if (_services is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else if (_services is IDisposable disposable)
            disposable.Dispose();
    }

    [Test]
    public async Task FindAsync_WithAllowedTimestampColumn_FiltersWorkflowInstances()
    {
        var timestamp = new DateTimeOffset(2026, 5, 20, 10, 0, 0, TimeSpan.Zero);
        await _workflowInstanceStore.AddAsync(CreateWorkflowInstance("matching", timestamp));
        await _workflowInstanceStore.AddAsync(CreateWorkflowInstance("older", timestamp.AddDays(-1)));

        var result = await _workflowInstanceFinder.FindAsync(new()
        {
            TimestampFilters =
            [
                new()
                {
                    Column = nameof(WorkflowInstance.CreatedAt),
                    Operator = TimestampFilterOperator.GreaterThanOrEqual,
                    Timestamp = timestamp
                }
            ]
        });

        var workflowInstanceId = await Assert.That(result).HasSingleItem();
        await Assert.That(workflowInstanceId).IsEqualTo("matching");
    }

    [Test]
    public async Task FindAsync_WithInjectedTimestampColumn_RejectsColumn()
    {
        var timestamp = new DateTimeOffset(2026, 5, 20, 10, 0, 0, TimeSpan.Zero);
        await _workflowInstanceStore.AddAsync(CreateWorkflowInstance("matching", timestamp));

        var exception = await Assert.ThrowsExactlyAsync<ArgumentException>(() => _workflowInstanceFinder.FindAsync(new()
        {
            TimestampFilters =
            [
                new()
                {
                    Column = "CreatedAt == @0 || Id != null",
                    Operator = TimestampFilterOperator.Is,
                    Timestamp = timestamp
                }
            ]
        }));

        await Assert.That(exception!.Message).Contains("Invalid timestamp filter column", StringComparison.CurrentCulture);
    }

    private static WorkflowInstance CreateWorkflowInstance(string id, DateTimeOffset createdAt)
    {
        return new()
        {
            Id = id,
            DefinitionId = "definition",
            DefinitionVersionId = "definition-version",
            Version = 1,
            WorkflowState = new WorkflowState
            {
                Id = id,
                DefinitionId = "definition",
                DefinitionVersionId = "definition-version",
                DefinitionVersion = 1,
                Status = WorkflowStatus.Running,
                SubStatus = WorkflowSubStatus.Suspended,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            },
            Status = WorkflowStatus.Running,
            SubStatus = WorkflowSubStatus.Suspended,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }
}
