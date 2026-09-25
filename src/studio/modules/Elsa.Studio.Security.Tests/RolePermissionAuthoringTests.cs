using Elsa.Studio.Security.Models;
using Xunit;

namespace Elsa.Studio.Security.Tests;

public sealed class RolePermissionAuthoringTests
{
    private static readonly PermissionResourceDescriptor[] Catalog =
    [
        new()
        {
            Resource = "workflows/definitions",
            SupportedVerbs = ["view", "update"],
            DisplayName = "Definitions",
            Category = "Workflows",
            Verified = true
        },
        new()
        {
            Resource = "workflows/instances",
            SupportedVerbs = ["view", "retry"],
            DisplayName = "Instances",
            Category = "Workflows",
            Verified = false
        }
    ];

    [Fact]
    public void NormalizePermissionsTrimsDeduplicatesOrdinallyAndSortsDeterministically()
    {
        var result = RolePermissionAuthoring.NormalizePermissions(
            [" workflows/instances:view ", "workflows/definitions:update", "workflows/instances:view", " ", "A", "a"]);

        Assert.Equal(["A", "a", "workflows/definitions:update", "workflows/instances:view"], result);
    }

    [Fact]
    public void NormalizePermissionsPreservesTheBareGlobalWildcardAsAStoredGrant()
    {
        var result = RolePermissionAuthoring.NormalizePermissions([" * ", "*"]);

        Assert.Equal(["*"], result);
        Assert.True(RolePermissionAuthoring.TryParse(result[0], out var pattern));
        Assert.True(RolePermissionAuthoring.Matches(pattern, "new/module", "future-verb"));
    }

    [Theory]
    [InlineData("*", "*", "*", "*")]
    [InlineData("workflows/*:view", "workflows/*", "view", "workflows/instances")]
    [InlineData("workflows/instances:*", "workflows/instances", "*", "workflows/instances")]
    public void TryParseUnderstandsGlobalResourceSubtreeAndVerbWildcards(string value, string resource, string verb, string coveredResource)
    {
        Assert.True(RolePermissionAuthoring.TryParse(value, out var pattern));
        Assert.Equal(resource, pattern.Resource);
        Assert.Equal(verb, pattern.Verb);
        Assert.True(RolePermissionAuthoring.IsValidPattern(pattern));
        Assert.True(RolePermissionAuthoring.Matches(pattern, coveredResource, "view"));
    }

    [Theory]
    [InlineData("workflows*:view")]
    [InlineData("workflows/definitions:vi*ew")]
    [InlineData("unknown: view")]
    public void InvalidOrUnknownGrantsRemainUnresolved(string value)
    {
        Assert.False(RolePermissionAuthoring.IsRecognized(value, Catalog, out var kind));
        Assert.Equal(RolePermissionGrantKind.Unresolved, kind);
    }

    [Fact]
    public void RecognizedExactAndWildcardGrantsAreClassifiedWithoutCatalogMaterialization()
    {
        Assert.True(RolePermissionAuthoring.IsRecognized("workflows/definitions:view", Catalog, out var exactKind));
        Assert.Equal(RolePermissionGrantKind.Exact, exactKind);

        Assert.True(RolePermissionAuthoring.IsRecognized("workflows/*:view", Catalog, out var wildcardKind));
        Assert.Equal(RolePermissionGrantKind.Wildcard, wildcardKind);

        Assert.True(RolePermissionAuthoring.IsCoveredByWildcard("workflows/instances", "view", ["workflows/*:view"]));
        Assert.False(RolePermissionAuthoring.IsCoveredByWildcard("workflows/instances", "retry", ["workflows/*:view"]));
    }

    [Fact]
    public void ConcreteGrantsContainOnlyCurrentDescriptorsAndVerbs()
    {
        var result = RolePermissionAuthoring.GetConcreteGrants(Catalog);

        Assert.Equal(
            [
                "workflows/definitions:view",
                "workflows/definitions:update",
                "workflows/instances:view",
                "workflows/instances:retry"
            ],
            result);
    }
}
