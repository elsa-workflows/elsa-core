using Elsa.Permissions;

namespace Elsa.Api.Common.UnitTests.Permissions;

public class PermissionDescriptorRegistryTests
{
    private sealed class Provider(params PermissionDescriptor[] descriptors) : IPermissionDescriptorProvider
    {
        public IEnumerable<PermissionDescriptor> GetDescriptors() => descriptors;
    }

    private static PermissionDescriptor Descriptor(string resource, params string[] verbs) =>
        new(resource, verbs.Length == 0 ? ["view"] : verbs, resource, $"Access {resource}.", "Test");

    private static DefaultPermissionDescriptorRegistry Registry(params IPermissionDescriptorProvider[] providers) => new(providers);

    [Fact]
    public void ComposesProvidersAndOrdersByResource()
    {
        var registry = Registry(
            new Provider(Descriptor("workflows/instances"), Descriptor("dashboard")),
            new Provider(Descriptor("identity/users")));

        Assert.Equal(["dashboard", "identity/users", "workflows/instances"], registry.List().Select(x => x.Resource));
    }

    [Fact]
    public void FirstRegistrationWinsForADuplicateResource()
    {
        var registry = Registry(new Provider(Descriptor("secrets", "view")), new Provider(Descriptor("secrets", "delete")));

        Assert.Single(registry.List());
        Assert.Equal(["view"], registry.Find("secrets")!.SupportedVerbs);
    }

    [Fact]
    public void MarksVerbsOutsideTheRecommendedCoreSet()
    {
        var descriptor = Descriptor("workflows/definitions", "view", "write", "publish", "retract");

        Assert.Equal(["publish", "retract"], descriptor.NonCoreVerbs);
        Assert.True(descriptor.Supports("write"));
        Assert.False(descriptor.Supports("quarantine"));
    }

    [Fact]
    public void ReachReportsWhatAWildcardCoversToday()
    {
        var registry = Registry(new Provider(
            Descriptor("workflows/definitions"),
            Descriptor("workflows/definitions/versions"),
            Descriptor("workflows/instances"),
            Descriptor("identity/users")));

        Assert.Equal(
            ["workflows/definitions", "workflows/definitions/versions", "workflows/instances"],
            registry.Reach("workflows/*"));
        Assert.Equal(["workflows/definitions", "workflows/definitions/versions"], registry.Reach("workflows/definitions/*"));
        Assert.Equal(4, registry.Reach("*").Count);
    }

    [Fact]
    public void ReachIsEmptyForAPatternMatchingNothing()
    {
        // A grant naming a module that is not installed is valid and simply covers nothing today.
        var registry = Registry(new Provider(Descriptor("dashboard")));

        Assert.Empty(registry.Reach("not-installed/*"));
    }

    [Fact]
    public void DescriptorsWithoutAResourceAreDropped()
    {
        var registry = Registry(new Provider(Descriptor("dashboard"), new("  ", ["view"], "", "", "Test")));

        Assert.Single(registry.List());
    }
}
