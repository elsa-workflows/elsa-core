using System.DirectoryServices.Protocols;
using Elsa.Extensions;
using Elsa.Ldap.Activities;
using Elsa.Ldap.Contracts;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Elsa.Ldap.UnitTests.Activities;

public class CompareLdapEntryTests
{
    [Fact]
    public async Task CompareLdapEntry_ComparesAttributesAndReturnsSuccessIfMatched()
    {
        // Arrange
        var activity = new CompareLdapEntry
        {
            ConnectionName = new("MyConnection"),
            EntryDn = new("cn=test,dc=example,dc=org"),
            Attribute = new(new DirectoryAttribute("cn", "test")),
        };

        var ldapConnectionMock = new Mock<ILdapConnection>();
        ldapConnectionMock.Setup(m => m.SendRequestAsync(It.IsAny<CompareRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestHelpers.CreateCompareResponse(ResultCode.CompareTrue));

        var ldapConnectionFactoryMock = new Mock<ILdapConnectionFactory>();
        ldapConnectionFactoryMock.Setup(m => m.CreateConnection(It.IsAny<string?>()))
            .Returns(ldapConnectionMock.Object);

        // Act
        var fixture = new ActivityTestFixture(activity);
        fixture.Services.AddScoped(_ => ldapConnectionFactoryMock.Object);

        var context = await fixture.ExecuteAsync();

        // Assert
        Assert.Equal(ActivityStatus.Completed, context.Status);
        Assert.True(context.HasOutcome("True"));
        Assert.False(context.HasOutcome("False"));
        Assert.True(context.GetActivityOutput(() => activity.Result) as bool?);
        Assert.Contains(context.JournalData, x => x.Key == "ResultCode" && (ResultCode)x.Value == ResultCode.CompareTrue);
        Assert.Contains(context.JournalData, x => x.Key == "ComparedEntry" && (string)x.Value == "cn=test,dc=example,dc=org");

        ldapConnectionMock.Verify(m => m.SendRequestAsync(
            It.Is<CompareRequest>(r => r.DistinguishedName == "cn=test,dc=example,dc=org"
                && r.Assertion.Name == "cn"
                && (string)r.Assertion[0] == "test")));
    }

    [Theory]
    [InlineData(ResultCode.EntryAlreadyExists)]
    [InlineData(ResultCode.InsufficientAccessRights)]
    [InlineData(ResultCode.InvalidAttributeSyntax)]
    [InlineData(ResultCode.NamingViolation)]
    [InlineData(ResultCode.Other)]
    public async Task CompareLdapEntry_ReturnsErrorWhenServerRespondsWithNonSuccess(ResultCode resultCode)
    {
        // Arrange
        var activity = new CompareLdapEntry
        {
            ConnectionName = new("MyConnection"),
            EntryDn = new("cn=test,dc=example,dc=org"),
            Attribute = new(new DirectoryAttribute("cn", "test")),
        };

        var ldapConnectionMock = new Mock<ILdapConnection>();
        ldapConnectionMock.Setup(m => m.SendRequestAsync(It.IsAny<CompareRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestHelpers.CreateCompareResponse(resultCode));

        var ldapConnectionFactoryMock = new Mock<ILdapConnectionFactory>();
        ldapConnectionFactoryMock.Setup(m => m.CreateConnection(It.IsAny<string?>()))
            .Returns(ldapConnectionMock.Object);

        // Act
        var fixture = new ActivityTestFixture(activity);
        fixture.Services.AddScoped(_ => ldapConnectionFactoryMock.Object);

        var context = await fixture.ExecuteAsync();

        // Assert
        Assert.Equal(ActivityStatus.Completed, context.Status);
        Assert.False(context.HasOutcome("True"));
        Assert.True(context.HasOutcome("False"));
        Assert.False(context.GetActivityOutput(() => activity.Result) as bool?);
    }
}
