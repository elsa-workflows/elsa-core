using Elsa.Authorization;

namespace Elsa.Api.Common.UnitTests.Authorization;

public class PermissionTests
{
    [Theory]
    [InlineData("workflows/definitions:view", "workflows/definitions", "view")]
    [InlineData("secrets:write", "secrets", "write")]
    [InlineData("workflows/*:view", "workflows/*", "view")]
    [InlineData("workflows/definitions:*", "workflows/definitions", "*")]
    [InlineData("*:*", "*", "*")]
    [InlineData("  secrets:view  ", "secrets", "view")]
    public void ParsesWellFormedPermissions(string value, string resource, string verb)
    {
        Assert.True(Permission.TryParse(value, out var permission));
        Assert.Equal(new Permission(resource, verb), permission);
        Assert.Equal($"{resource}:{verb}", permission.ToString());
    }

    [Fact]
    public void ABareWildcardNormalizesToTheWholeVocabulary()
    {
        // A parsing rule, not an evaluation special case: it is what lets a stored or seeded "*" keep
        // authorizing across the vocabulary migration without a lock-out window.
        Assert.True(Permission.TryParse("*", out var permission));
        Assert.Equal(Permission.All, permission);
        Assert.Equal("*:*", permission.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("workflows/definitions")]      // no verb
    [InlineData(":view")]                       // no resource
    [InlineData("workflows/definitions:")]      // empty verb
    [InlineData("workflows:definitions:view")]  // verb may not contain the separator
    [InlineData("workflows:defs/view")]         // verb may not contain a path separator
    [InlineData("workflows/definitions:view,create")] // a comma can never appear: persistence joins on it
    public void RejectsMalformedPermissions(string? value)
    {
        Assert.False(Permission.TryParse(value, out _));
    }

    [Fact]
    public void ParseThrowsOnMalformedInput()
    {
        Assert.Throws<FormatException>(() => Permission.Parse("workflows/definitions"));
    }

    [Theory]
    [InlineData("*:*", true, true, false)]
    [InlineData("workflows/*:view", false, false, true)]
    [InlineData("workflows/definitions:*", false, true, false)]
    [InlineData("workflows/definitions:view", false, false, false)]
    public void ClassifiesWildcards(string value, bool resourceWildcard, bool verbWildcard, bool subtree)
    {
        var permission = Permission.Parse(value);

        Assert.Equal(resourceWildcard, permission.IsResourceWildcard);
        Assert.Equal(verbWildcard, permission.IsVerbWildcard);
        Assert.Equal(subtree, permission.IsSubtree);
        Assert.Equal(resourceWildcard || verbWildcard || subtree, permission.HasWildcard);
    }

    [Theory]
    [InlineData("workflows/definitions:view", true)]
    [InlineData("workflows/*:view", true)]
    [InlineData("workflows/definitions:*", true)]
    [InlineData("*:*", true)]
    [InlineData("workflows*:delete", false)]        // missing slash: not a subtree pattern
    [InlineData("work*/foo:view", false)]           // embedded wildcard mid-resource
    [InlineData("work*/definitions/*:view", false)] // trailing '/*' does not redeem an embedded '*'
    [InlineData("workflows/*/versions:view", false)] // '*' as a middle segment
    [InlineData("workflows:del*", false)]           // embedded wildcard in the verb
    public void RecognizesWildcardsTheMatcherNeverSatisfies(string value, bool valid)
    {
        // Such strings parse — TryParse stays lenient for stored roles — but validation paths reject them,
        // because a pattern that matches nothing in a deny list silently stops denying.
        Assert.Equal(valid, Permission.Parse(value).IsValidPattern);
    }
}
