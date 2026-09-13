using Elsa.Authorization;
using System.Threading.Tasks;

namespace Elsa.Api.Common.UnitTests.Authorization;

public class PermissionTests
{
    [Test]
    [Arguments("workflows/definitions:view", "workflows/definitions", "view")]
    [Arguments("secrets:write", "secrets", "write")]
    [Arguments("workflows/*:view", "workflows/*", "view")]
    [Arguments("workflows/definitions:*", "workflows/definitions", "*")]
    [Arguments("*:*", "*", "*")]
    [Arguments("  secrets:view  ", "secrets", "view")]
    public async Task ParsesWellFormedPermissions(string value, string resource, string verb)
    {
        await Assert.That(Permission.TryParse(value, out var permission)).IsTrue();
        await Assert.That(permission).IsEqualTo(new Permission(resource, verb));
        await Assert.That(permission.ToString()).IsEqualTo($"{resource}:{verb}");
    }

    [Test]
    public async Task ABareWildcardNormalizesToTheWholeVocabulary()
    {
        // A parsing rule, not an evaluation special case: it is what lets a stored or seeded "*" keep
        // authorizing across the vocabulary migration without a lock-out window.
        await Assert.That(Permission.TryParse("*", out var permission)).IsTrue();
        await Assert.That(permission).IsEqualTo(Permission.All);
        await Assert.That(permission.ToString()).IsEqualTo("*:*");
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments("workflows/definitions")]      // no verb
    [Arguments(":view")]                       // no resource
    [Arguments("workflows/definitions:")]      // empty verb
    [Arguments("workflows:definitions:view")]  // verb may not contain the separator
    [Arguments("workflows:defs/view")]         // verb may not contain a path separator
    [Arguments("workflows/definitions:view,create")] // a comma can never appear: persistence joins on it
    public async Task RejectsMalformedPermissions(string? value)
    {
        await Assert.That(Permission.TryParse(value, out _)).IsFalse();
    }

    [Test]
    public void ParseThrowsOnMalformedInput()
    {
        Assert.ThrowsExactly<FormatException>(() => Permission.Parse("workflows/definitions"));
    }

    [Test]
    [Arguments("*:*", true, true, false)]
    [Arguments("workflows/*:view", false, false, true)]
    [Arguments("workflows/definitions:*", false, true, false)]
    [Arguments("workflows/definitions:view", false, false, false)]
    public async Task ClassifiesWildcards(string value, bool resourceWildcard, bool verbWildcard, bool subtree)
    {
        var permission = Permission.Parse(value);

        await Assert.That(permission.IsResourceWildcard).IsEqualTo(resourceWildcard);
        await Assert.That(permission.IsVerbWildcard).IsEqualTo(verbWildcard);
        await Assert.That(permission.IsSubtree).IsEqualTo(subtree);
        await Assert.That(permission.HasWildcard).IsEqualTo(resourceWildcard || verbWildcard || subtree);
    }

    [Test]
    [Arguments("workflows/definitions:view", true)]
    [Arguments("workflows/*:view", true)]
    [Arguments("workflows/definitions:*", true)]
    [Arguments("*:*", true)]
    [Arguments("workflows*:delete", false)]        // missing slash: not a subtree pattern
    [Arguments("work*/foo:view", false)]           // embedded wildcard mid-resource
    [Arguments("work*/definitions/*:view", false)] // trailing '/*' does not redeem an embedded '*'
    [Arguments("workflows/*/versions:view", false)] // '*' as a middle segment
    [Arguments("workflows:del*", false)]           // embedded wildcard in the verb
    public async Task RecognizesWildcardsTheMatcherNeverSatisfies(string value, bool valid)
    {
        // Such strings parse — TryParse stays lenient for stored roles — but validation paths reject them,
        // because a pattern that matches nothing in a deny list silently stops denying.
        await Assert.That(Permission.Parse(value).IsValidPattern).IsEqualTo(valid);
    }
}
