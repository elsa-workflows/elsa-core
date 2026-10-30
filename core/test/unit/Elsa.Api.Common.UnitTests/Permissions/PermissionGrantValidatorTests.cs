using Elsa.Permissions;

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

    [Theory]
    [InlineData("workflows/definitions:view")]
    [InlineData("workflows/definitions:publish")]
    [InlineData("secrets:write")]
    public void AcceptsConcreteGrantsTheCatalogKnows(string permission)
    {
        Assert.True(Validator.Validate([permission]).IsValid);
    }

    [Theory]
    [InlineData("workflows/*:view")]
    [InlineData("workflows/definitions:*")]
    [InlineData("*:*")]
    [InlineData("*")]
    public void AcceptsWildcards(string permission)
    {
        // Wildcards are validated structurally. `workflows/*` matches no single descriptor and `*` is
        // deliberately absent from every supported-verb list, so catalog validation would reject exactly
        // the grants the hierarchy exists to make possible.
        Assert.True(Validator.Validate([permission]).IsValid);
    }

    [Fact]
    public void AcceptsAWildcardThatCurrentlyMatchesNothing()
    {
        // A grant naming a module that is not installed yet must survive: installing it later is what
        // gives the grant meaning.
        Assert.True(Validator.Validate(["not-installed/*:view"]).IsValid);
    }

    [Fact]
    public void RejectsAConcreteResourceNoModuleRegisters()
    {
        var result = Validator.Validate(["invented/resource:view"]);

        Assert.False(result.IsValid);
        Assert.Contains("No module registers", result.Errors.Single().Reason);
    }

    [Fact]
    public void RejectsAVerbTheResourceDoesNotSupport()
    {
        var result = Validator.Validate(["secrets:publish"]);

        Assert.False(result.IsValid);
        Assert.Contains("does not support the verb", result.Errors.Single().Reason);
        Assert.Contains("view, write", result.Errors.Single().Reason);
    }

    [Theory]
    [InlineData("workflows*:delete")]
    [InlineData("work*/foo/*:view")]
    [InlineData("workflows/*/instances:view")]
    [InlineData("workflows/definitions:vi*w")]
    public void RejectsWildcardsTheMatcherNeverSatisfies(string permission)
    {
        // These parse, but the matcher never satisfies them; accepting them would persist a grant
        // that silently reaches nothing.
        var result = Validator.Validate([permission]);

        Assert.False(result.IsValid);
        Assert.Contains("would match nothing", result.Errors.Single().Reason);
    }

    [Theory]
    [InlineData("not a permission")]
    [InlineData("workflows/definitions")]
    [InlineData("workflows/definitions:view,create")]
    public void RejectsMalformedPermissions(string permission)
    {
        var result = Validator.Validate([permission]);

        Assert.False(result.IsValid);
        Assert.Contains("well-formed", result.Errors.Single().Reason);
    }

    [Fact]
    public void ReportsEveryOffendingGrantRatherThanTheFirst()
    {
        var result = Validator.Validate(["secrets:publish", "invented:view", "workflows/definitions:view"]);

        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void TreatsNullAndEmptyEntriesAsNothingToValidate()
    {
        Assert.True(Validator.Validate(null).IsValid);
        Assert.True(Validator.Validate([]).IsValid);
        Assert.True(Validator.Validate(["", "   "]).IsValid);
    }
}
