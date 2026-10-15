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
using Xunit.Abstractions;

namespace Elsa.Alterations.IntegrationTests;

public class WorkflowInstanceFinderTimestampFilterTests : IDisposable
{
    private readonly IServiceProvider _services;
    private readonly IWorkflowInstanceFinder _workflowInstanceFinder;
    private readonly IWorkflowInstanceStore _workflowInstanceStore;

    public WorkflowInstanceFinderTimestampFilterTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureElsa(elsa => elsa.UseAlterations())
            .Build();
        
        _workflowInstanceFinder = _services.GetRequiredService<IWorkflowInstanceFinder>();
        _workflowInstanceStore = _services.GetRequiredService<IWorkflowInstanceStore>();
    }

    public void Dispose()
    {
        (_services as IDisposable)?.Dispose();
    }

    [Fact]
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

        var workflowInstanceId = Assert.Single(result);
        Assert.Equal("matching", workflowInstanceId);
    }

    [Fact]
    public async Task FindAsync_WithInjectedTimestampColumn_RejectsColumn()
    {
        var timestamp = new DateTimeOffset(2026, 5, 20, 10, 0, 0, TimeSpan.Zero);
        await _workflowInstanceStore.AddAsync(CreateWorkflowInstance("matching", timestamp));

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _workflowInstanceFinder.FindAsync(new()
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

        Assert.Contains("Invalid timestamp filter column", exception.Message);
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
