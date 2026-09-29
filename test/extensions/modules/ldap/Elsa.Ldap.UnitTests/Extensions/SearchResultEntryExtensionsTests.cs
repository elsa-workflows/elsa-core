using System.DirectoryServices.Protocols;
using Elsa.Ldap.Extensions;

namespace Elsa.Ldap.UnitTests.Extensions;

public class SearchResultEntryExtensionsTests
{
    [Fact]
    public void MapToAttributesDictionary_ShouldReturnEmptyDictionary_WhenNoAttributes()
    {
        // Arrange
        var entry = TestHelpers.CreateSearchResultEntry("cn=test");

        // Act
        var result = entry.MapToAttributesDictionary();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void MapToAttributesDictionary_ShouldMapAttributesCorrectly()
    {
        // Arrange
        var entry = TestHelpers.CreateSearchResultEntry("cn=test",
            new DirectoryAttribute("objectClass", "top", "person"),
            new DirectoryAttribute("cn", "test"),
            new DirectoryAttribute("sn", "User"),
            new DirectoryAttribute("description", "", "A test user")
        );

        // Act
        var result = entry.MapToAttributesDictionary();

        // Assert
        Assert.Equal(4, result.Count);
        Assert.True(result.ContainsKey("objectClass"));
        Assert.Equal(new[] { "top", "person" }, result["objectClass"]);
        Assert.True(result.ContainsKey("cn"));
        Assert.Equal(new[] { "test" }, result["cn"]);
        Assert.True(result.ContainsKey("sn"));
        Assert.Equal(new[] { "User" }, result["sn"]);
        Assert.True(result.ContainsKey("description"));
        Assert.Equal(new[] { string.Empty, "A test user" }, result["description"]);
    }
}