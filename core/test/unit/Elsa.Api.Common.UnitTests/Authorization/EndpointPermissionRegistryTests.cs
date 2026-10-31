using Elsa.Authorization;

namespace Elsa.Api.Common.UnitTests.Authorization;

/// <summary>
/// The registry reports every declaration through its requirement accessors, while the single-permission accessors that
/// predate "any of" requirements keep reporting exactly what they did: the one permission an endpoint requires.
/// </summary>
public class EndpointPermissionRegistryTests
{
    private static readonly Permission Alpha = new("tests/alpha", CoreVerbs.View);
    private static readonly Permission Beta = new("tests/beta", CoreVerbs.View);

    [Fact]
    public void ASinglePermission_IsReportedByEveryAccessor()
    {
        EndpointPermissionRegistry.Record(typeof(SinglePermissionEndpoint), Alpha);

        Assert.Equal(Alpha, EndpointPermissionRegistry.Find(typeof(SinglePermissionEndpoint)));
        Assert.Equal(Alpha, EndpointPermissionRegistry.All[typeof(SinglePermissionEndpoint)]);
        Assert.Equal([Alpha], EndpointPermissionRegistry.FindRequirement(typeof(SinglePermissionEndpoint))!.AnyOf);
        Assert.Equal([Alpha], EndpointPermissionRegistry.AllRequirements[typeof(SinglePermissionEndpoint)].AnyOf);
    }

    [Fact]
    public void AnAnyOfRequirement_IsReportedOnlyByTheRequirementAccessors()
    {
        EndpointPermissionRegistry.Record(typeof(AnyOfEndpoint), new EndpointPermissionRequirement([Alpha, Beta]));

        Assert.Equal([Alpha, Beta], EndpointPermissionRegistry.FindRequirement(typeof(AnyOfEndpoint))!.AnyOf);
        Assert.Equal([Alpha, Beta], EndpointPermissionRegistry.AllRequirements[typeof(AnyOfEndpoint)].AnyOf);

        // A single permission cannot express a choice, so these report nothing rather than one of the two.
        Assert.Null(EndpointPermissionRegistry.Find(typeof(AnyOfEndpoint)));
        Assert.False(EndpointPermissionRegistry.All.ContainsKey(typeof(AnyOfEndpoint)));
    }

    [Fact]
    public void RecordingAgain_ReplacesTheEarlierDeclarationInEveryAccessor()
    {
        EndpointPermissionRegistry.Record(typeof(RedeclaredEndpoint), Alpha);
        EndpointPermissionRegistry.Record(typeof(RedeclaredEndpoint), new EndpointPermissionRequirement([Alpha, Beta]));

        Assert.Null(EndpointPermissionRegistry.Find(typeof(RedeclaredEndpoint)));
        Assert.False(EndpointPermissionRegistry.All.ContainsKey(typeof(RedeclaredEndpoint)));

        EndpointPermissionRegistry.Record(typeof(RedeclaredEndpoint), Beta);

        Assert.Equal(Beta, EndpointPermissionRegistry.Find(typeof(RedeclaredEndpoint)));
        Assert.Equal([Beta], EndpointPermissionRegistry.FindRequirement(typeof(RedeclaredEndpoint))!.AnyOf);
    }

    [Fact]
    public void ARequirement_CollapsesDuplicatePermissions() =>
        Assert.Equal([Alpha], new EndpointPermissionRequirement([Alpha, Alpha]).AnyOf);

    [Fact]
    public void ARequirementWithNoPermissions_IsRejected() =>
        Assert.Throws<ArgumentException>(() => new EndpointPermissionRequirement([]));

    // The registry is process-global, so each test records against a type of its own.
    private sealed class SinglePermissionEndpoint;

    private sealed class AnyOfEndpoint;

    private sealed class RedeclaredEndpoint;
}
