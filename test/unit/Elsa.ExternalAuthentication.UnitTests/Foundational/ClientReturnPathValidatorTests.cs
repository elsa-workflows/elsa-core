using Elsa.ExternalAuthentication.Validation;
using System.Threading.Tasks;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class ClientReturnPathValidatorTests
{
    [Test]
    [Arguments("https://attacker.example/sign-in")]
    [Arguments("//attacker.example/sign-in")]
    [Arguments("/\\attacker.example/sign-in")]
    [Arguments("/workflows\r\nLocation: https://attacker.example")]
    [Arguments("/%2f%2fattacker.example/sign-in")]
    [Arguments("/%5c%5cattacker.example/sign-in")]
    public async Task RejectsUnsafeReturnPaths(string returnPath)
    {
        var isValid = ClientReturnPathValidator.TryValidate(returnPath, out var validatedReturnPath);

        await Assert.That(isValid).IsFalse();
        await Assert.That(validatedReturnPath).IsEqualTo(ClientReturnPathValidator.DefaultReturnPath);
    }

    [Test]
    public async Task RejectsOverlengthReturnPaths()
    {
        var returnPath = "/" + new string('a', ClientReturnPathValidator.MaximumLength);

        var isValid = ClientReturnPathValidator.TryValidate(returnPath, out var validatedReturnPath);

        await Assert.That(isValid).IsFalse();
        await Assert.That(validatedReturnPath).IsEqualTo(ClientReturnPathValidator.DefaultReturnPath);
    }

    [Test]
    public async Task AcceptsClientLocalReturnPaths()
    {
        const string returnPath = "/workflows?definition=order-entry#versions";

        var isValid = ClientReturnPathValidator.TryValidate(returnPath, out var validatedReturnPath);

        await Assert.That(isValid).IsTrue();
        await Assert.That(validatedReturnPath).IsEqualTo(returnPath);
        await Assert.That(ClientReturnPathValidator.GetSafeReturnPath(returnPath)).IsEqualTo(returnPath);
    }

    [Test]
    [Arguments("/workflows", true)]
    [Arguments("/workflows/", true)]
    [Arguments("/workflows/order-entry?version=1", true)]
    [Arguments("/settings", false)]
    [Arguments("/workflows-evil", false)]
    public async Task EnforcesClientSpecificPathPrefixes(string returnPath, bool expected)
    {
        var allowedPrefixes = new HashSet<string>(StringComparer.Ordinal) { "/workflows" };

        var isValid = ClientReturnPathValidator.TryValidateForClient(returnPath, allowedPrefixes, out var validatedReturnPath);

        await Assert.That(isValid).IsEqualTo(expected);
        await Assert.That(validatedReturnPath).IsEqualTo(expected ? returnPath : ClientReturnPathValidator.DefaultReturnPath);
    }
}
