using Elsa.Secrets.Services;
using Xunit;

namespace Elsa.Secrets.UnitTests;

public class DefaultSecretNameValidatorTests
{
    private readonly DefaultSecretNameValidator _validator = new();

    [Theory]
    [InlineData("ab")]
    [InlineData("Smtp:Password")]
    [InlineData("secret.name-1_value:prod")]
    public void IsValid_ReturnsTrue_ForValidTechnicalNames(string name)
    {
        var isValid = _validator.IsValid(name, out var error);

        Assert.True(isValid);
        Assert.Null(error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void IsValid_ReturnsRequiredError_ForMissingTechnicalNames(string? name)
    {
        var isValid = _validator.IsValid(name, out var error);

        Assert.False(isValid);
        Assert.Equal("Secret name is required.", error);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("1secret")]
    [InlineData("secret name")]
    [InlineData("secret/name")]
    public void IsValid_ReturnsFormatError_ForInvalidTechnicalNames(string name)
    {
        var isValid = _validator.IsValid(name, out var error);

        Assert.False(isValid);
        Assert.Equal("Secret name must be 2-200 characters, start with a letter, and contain only letters, numbers, dots, dashes, underscores, and colons.", error);
    }

    [Fact]
    public void IsValid_AcceptsTrimmedTechnicalName()
    {
        var isValid = _validator.IsValid("  Smtp:Password  ", out var error);

        Assert.True(isValid);
        Assert.Null(error);
    }

    [Fact]
    public void IsValid_RejectsNamesLongerThanTwoHundredCharacters()
    {
        var name = $"a{new string('b', 200)}";

        var isValid = _validator.IsValid(name, out var error);

        Assert.False(isValid);
        Assert.NotNull(error);
    }

    [Fact]
    public void Normalize_TrimsAndLowercasesTechnicalName()
    {
        var normalized = _validator.Normalize("  Smtp:Password  ");

        Assert.Equal("smtp:password", normalized);
    }
}
