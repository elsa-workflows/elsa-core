using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Workflows.Core.UnitTests.Models;

public class ActivityOutputRegisterTests
{
    private const string ProducerId = "Producer";

    [Fact(DisplayName = "The scoped lookup returns the most recent value recorded in one of the specified containers")]
    public async Task ScopedLookup_ReturnsMostRecentInScopeValue()
    {
        var scope = await CreateScopeAsync();
        await scope.RecordAsync(scope.BranchA, 1);
        await scope.RecordAsync(scope.BranchB, 2);
        await scope.RecordAsync(scope.BranchA, 3);

        Assert.True(scope.Register.TryFindOutputByActivityId(ProducerId, null, [scope.Root.Id, scope.BranchA.Id], out var branchAValue));
        Assert.True(scope.Register.TryFindOutputByActivityId(ProducerId, null, [scope.Root.Id, scope.BranchB.Id], out var branchBValue));
        Assert.Equal(3, branchAValue);
        Assert.Equal(2, branchBValue);
        Assert.Equal(3, scope.Register.FindOutputByActivityId(ProducerId));
    }

    [Fact(DisplayName = "The scoped lookup returns an in-scope null instead of a value from another container")]
    public async Task ScopedLookup_InScopeNull_ReturnsTrueWithNull()
    {
        var scope = await CreateScopeAsync();
        await scope.RecordAsync(scope.BranchA, null);
        await scope.RecordAsync(scope.BranchB, 2);

        var found = scope.Register.TryFindOutputByActivityId(ProducerId, null, [scope.Root.Id, scope.BranchA.Id], out var value);

        Assert.True(found);
        Assert.Null(value);
    }

    [Fact(DisplayName = "The scoped lookup returns false when the activity recorded nothing in the specified containers")]
    public async Task ScopedLookup_NoInScopeRecord_ReturnsFalse()
    {
        var scope = await CreateScopeAsync();
        await scope.RecordAsync(scope.BranchB, 2);

        var found = scope.Register.TryFindOutputByActivityId(ProducerId, null, [scope.Root.Id, scope.BranchA.Id], out var value);

        Assert.False(found);
        Assert.Null(value);
    }

    [Theory(DisplayName = "The scoped lookup treats a null output name as the default output name")]
    [InlineData(null, ActivityOutputRegister.DefaultOutputName)]
    [InlineData(ActivityOutputRegister.DefaultOutputName, null)]
    [InlineData(null, null)]
    public async Task ScopedLookup_NullOutputName_UsesDefaultOutputName(string? recordedOutputName, string? requestedOutputName)
    {
        var scope = await CreateScopeAsync();
        await scope.RecordAsync(scope.BranchA, 1, recordedOutputName);
        await scope.RecordAsync(scope.BranchA, 2, "Other");

        var found = scope.Register.TryFindOutputByActivityId(ProducerId, requestedOutputName, [scope.Root.Id, scope.BranchA.Id], out var value);

        Assert.True(found);
        Assert.Equal(1, value);
    }

    [Theory(DisplayName = "The scoped lookup throws when the container IDs are null, whether or not the activity recorded output")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ScopedLookup_NullContainerIds_Throws(bool recordOutput)
    {
        var scope = await CreateScopeAsync();
        if (recordOutput)
        {
            await scope.RecordAsync(scope.BranchA, 1);
        }

        var exception = Assert.Throws<ArgumentNullException>(() => scope.Register.TryFindOutputByActivityId(ProducerId, null, null!, out _));

        Assert.Equal("containerIds", exception.ParamName);
    }

    private static async Task<OutputScope> CreateScopeAsync()
    {
        var branchA = new Sequence { Id = "BranchA" };
        var branchB = new Sequence { Id = "BranchB" };
        var producer = new Sequence { Id = ProducerId };
        var root = new Sequence { Activities = { branchA, branchB, producer } };

        // The fixture's default identity generator returns the same ID for every context, which would put all records in one container.
        var identityGenerator = Substitute.For<IIdentityGenerator>();
        identityGenerator.GenerateId().Returns(_ => Guid.NewGuid().ToString());
        var rootContext = await new ActivityTestFixture(root)
            .ConfigureServices(services => services.AddSingleton(identityGenerator))
            .BuildAsync();
        var workflowExecutionContext = rootContext.WorkflowExecutionContext;
        var branchAContext = await workflowExecutionContext.CreateActivityExecutionContextAsync(branchA, new() { Owner = rootContext });
        var branchBContext = await workflowExecutionContext.CreateActivityExecutionContextAsync(branchB, new() { Owner = rootContext });

        return new(workflowExecutionContext.GetActivityOutputRegister(), rootContext, branchAContext, branchBContext, producer);
    }

    /// <summary>
    /// Root context with two branch contexts, BranchA and BranchB. The producer can run in either branch.
    /// </summary>
    private record OutputScope(ActivityOutputRegister Register, ActivityExecutionContext Root, ActivityExecutionContext BranchA, ActivityExecutionContext BranchB, IActivity Producer)
    {
        public async Task RecordAsync(ActivityExecutionContext branch, object? value, string? outputName = null)
        {
            var producerContext = await Root.WorkflowExecutionContext.CreateActivityExecutionContextAsync(Producer, new ActivityInvocationOptions { Owner = branch });
            Register.Record(producerContext, outputName, value);
        }
    }
}
