using Elsa.Permissions;
using System.Threading.Tasks;

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

    [Test]
    public async Task ComposesProvidersAndOrdersByResource()
    {
        var registry = Registry(
            new Provider(Descriptor("workflows/instances"), Descriptor("dashboard")),
            new Provider(Descriptor("identity/users")));

        await Assert.That(registry.List().Select(x => x.Resource)).IsEquivalentTo(["dashboard", "identity/users", "workflows/instances"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task FirstRegistrationWinsForADuplicateResource()
    {
        var registry = Registry(new Provider(Descriptor("secrets", "view")), new Provider(Descriptor("secrets", "delete")));

        await Assert.That(registry.List()).HasSingleItem();
        await Assert.That(registry.Find("secrets")!.SupportedVerbs).IsEquivalentTo(["view"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task MarksVerbsOutsideTheRecommendedCoreSet()
    {
        var descriptor = Descriptor("workflows/definitions", "view", "write", "publish", "retract");

        await Assert.That(descriptor.NonCoreVerbs).IsEquivalentTo(["publish", "retract"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(descriptor.Supports("write")).IsTrue();
        await Assert.That(descriptor.Supports("quarantine")).IsFalse();
    }

    [Test]
    public async Task ReachReportsWhatAWildcardCoversToday()
    {
        var registry = Registry(new Provider(
            Descriptor("workflows/definitions"),
            Descriptor("workflows/definitions/versions"),
            Descriptor("workflows/instances"),
            Descriptor("identity/users")));

        await Assert.That(registry.Reach("workflows/*")).IsEquivalentTo(["workflows/definitions", "workflows/definitions/versions", "workflows/instances"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(registry.Reach("workflows/definitions/*")).IsEquivalentTo(["workflows/definitions", "workflows/definitions/versions"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(registry.Reach("*").Count).IsEqualTo(4);
    }

    [Test]
    public async Task ReachIsEmptyForAPatternMatchingNothing()
    {
        // A grant naming a module that is not installed is valid and simply covers nothing today.
        var registry = Registry(new Provider(Descriptor("dashboard")));

        await Assert.That(registry.Reach("not-installed/*")).IsEmpty();
    }

    [Test]
    public async Task DescriptorsWithoutAResourceAreDropped()
    {
        var registry = Registry(new Provider(Descriptor("dashboard"), new("  ", ["view"], "", "", "Test")));

        await Assert.That(registry.List()).HasSingleItem();
    }
}
