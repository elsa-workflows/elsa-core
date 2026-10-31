using Elsa.ExternalAuthentication.Validation;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class ClientReturnPathValidatorTests
{
    [Theory]
    [InlineData("https://attacker.example/sign-in")]
    [InlineData("//attacker.example/sign-in")]
    [InlineData("/\\attacker.example/sign-in")]
    [InlineData("/workflows\r\nLocation: https://attacker.example")]
    [InlineData("/%2f%2fattacker.example/sign-in")]
    [InlineData("/%5c%5cattacker.example/sign-in")]
    public void RejectsUnsafeReturnPaths(string returnPath)
    {
        var isValid = ClientReturnPathValidator.TryValidate(returnPath, out var validatedReturnPath);

        Assert.False(isValid);
        Assert.Equal(ClientReturnPathValidator.DefaultReturnPath, validatedReturnPath);
    }

    [Fact]
    public void RejectsOverlengthReturnPaths()
    {
        var returnPath = "/" + new string('a', ClientReturnPathValidator.MaximumLength);

        var isValid = ClientReturnPathValidator.TryValidate(returnPath, out var validatedReturnPath);

        Assert.False(isValid);
        Assert.Equal(ClientReturnPathValidator.DefaultReturnPath, validatedReturnPath);
    }

    [Fact]
    public void AcceptsClientLocalReturnPaths()
    {
        const string returnPath = "/workflows?definition=order-entry#versions";

        var isValid = ClientReturnPathValidator.TryValidate(returnPath, out var validatedReturnPath);

        Assert.True(isValid);
        Assert.Equal(returnPath, validatedReturnPath);
        Assert.Equal(returnPath, ClientReturnPathValidator.GetSafeReturnPath(returnPath));
    }

    [Theory]
    [InlineData("/workflows", true)]
    [InlineData("/workflows/", true)]
    [InlineData("/workflows/order-entry?version=1", true)]
    [InlineData("/settings", false)]
    [InlineData("/workflows-evil", false)]
    public void EnforcesClientSpecificPathPrefixes(string returnPath, bool expected)
    {
        var allowedPrefixes = new HashSet<string>(StringComparer.Ordinal) { "/workflows" };

        var isValid = ClientReturnPathValidator.TryValidateForClient(returnPath, allowedPrefixes, out var validatedReturnPath);

        Assert.Equal(expected, isValid);
        Assert.Equal(expected ? returnPath : ClientReturnPathValidator.DefaultReturnPath, validatedReturnPath);
    }
}
