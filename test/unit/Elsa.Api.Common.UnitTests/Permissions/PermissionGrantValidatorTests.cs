using Elsa.Permissions;
using System.Threading.Tasks;

namespace Elsa.Api.Common.UnitTests.Permissions;

public class PermissionGrantValidatorTests
{
    private sealed class Provider(params PermissionDescriptor[] descriptors) : IPermissionDescriptorProvider
    {
        public IEnumerable<PermissionDescriptor> GetDescriptors() => descriptors;
    }

    private static readonly PermissionGrantValidator Validator = new(
        new DefaultPermissionDescriptorRegistry([
            new Provider(
                new("workflows/definitions", ["view", "write", "publish"], "Definitions", "", "Workflows"),
                new("workflows/instances", ["view", "cancel"], "Instances", "", "Workflows"),
                new("secrets", ["view", "write"], "Secrets", "", "Secrets"))
        ]));

    [Test]
    [Arguments("workflows/definitions:view")]
    [Arguments("workflows/definitions:publish")]
    [Arguments("secrets:write")]
    public async Task AcceptsConcreteGrantsTheCatalogKnows(string permission)
    {
        await Assert.That(Validator.Validate([permission]).IsValid).IsTrue();
    }

    [Test]
    [Arguments("workflows/*:view")]
    [Arguments("workflows/definitions:*")]
    [Arguments("*:*")]
    [Arguments("*")]
    public async Task AcceptsWildcards(string permission)
    {
        // Wildcards are validated structurally. `workflows/*` matches no single descriptor and `*` is
        // deliberately absent from every supported-verb list, so catalog validation would reject exactly
        // the grants the hierarchy exists to make possible.
        await Assert.That(Validator.Validate([permission]).IsValid).IsTrue();
    }

    [Test]
    public async Task AcceptsAWildcardThatCurrentlyMatchesNothing()
    {
        // A grant naming a module that is not installed yet must survive: installing it later is what
        // gives the grant meaning.
        await Assert.That(Validator.Validate(["not-installed/*:view"]).IsValid).IsTrue();
    }

    [Test]
    public async Task RejectsAConcreteResourceNoModuleRegisters()
    {
        var result = Validator.Validate(["invented/resource:view"]);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Single().Reason).Contains("No module registers");
    }

    [Test]
    public async Task RejectsAVerbTheResourceDoesNotSupport()
    {
        var result = Validator.Validate(["secrets:publish"]);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Single().Reason).Contains("does not support the verb");
        await Assert.That(result.Errors.Single().Reason).Contains("view, write");
    }

    [Test]
    [Arguments("workflows*:delete")]
    [Arguments("work*/foo/*:view")]
    [Arguments("workflows/*/instances:view")]
    [Arguments("workflows/definitions:vi*w")]
    public async Task RejectsWildcardsTheMatcherNeverSatisfies(string permission)
    {
        // These parse, but the matcher never satisfies them; accepting them would persist a grant
        // that silently reaches nothing.
        var result = Validator.Validate([permission]);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Single().Reason).Contains("would match nothing");
    }

    [Test]
    [Arguments("not a permission")]
    [Arguments("workflows/definitions")]
    [Arguments("workflows/definitions:view,create")]
    public async Task RejectsMalformedPermissions(string permission)
    {
        var result = Validator.Validate([permission]);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Single().Reason).Contains("well-formed");
    }

    [Test]
    public async Task ReportsEveryOffendingGrantRatherThanTheFirst()
    {
        var result = Validator.Validate(["secrets:publish", "invented:view", "workflows/definitions:view"]);

        await Assert.That(result.Errors.Count).IsEqualTo(2);
    }

    [Test]
    public async Task TreatsNullAndEmptyEntriesAsNothingToValidate()
    {
        await Assert.That(Validator.Validate(null).IsValid).IsTrue();
        await Assert.That(Validator.Validate([]).IsValid).IsTrue();
        await Assert.That(Validator.Validate(["", "   "]).IsValid).IsTrue();
    }
}