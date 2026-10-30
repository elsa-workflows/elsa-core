using System.DirectoryServices.Protocols;
using Elsa.Extensions;
using Elsa.Ldap.Activities;
using Elsa.Ldap.Contracts;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Elsa.Ldap.UnitTests.Activities;

public class AddLdapEntryTests
{
    [Fact]
    public async Task AddLdapEntry_CreatesEntry_ReturnsSuccessWhenServerRespondsWithSuccess()
    {
        // Arrange
        var activity = new AddLdapEntry
        {
            ConnectionName = new("MyConnection"),
            EntryDn = new("cn=test,dc=example,dc=org"),
            Attributes = new([
                new DirectoryAttribute("cn", "test"),
                new DirectoryAttribute("sn", "user"),
            ]),
        };

        var ldapConnectionMock = new Mock<ILdapConnection>();
        ldapConnectionMock.Setup(m => m.SendRequestAsync(It.IsAny<AddRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestHelpers.CreateAddResponse(ResultCode.Success));

        var ldapConnectionFactoryMock = new Mock<ILdapConnectionFactory>();
        ldapConnectionFactoryMock.Setup(m => m.CreateConnection(It.IsAny<string?>()))
            .Returns(ldapConnectionMock.Object);

        // Act
        var fixture = new ActivityTestFixture(activity);
        fixture.Services.AddScoped(_ => ldapConnectionFactoryMock.Object);

        var context = await fixture.ExecuteAsync();

        // Assert
        Assert.Equal(ActivityStatus.Completed, context.Status);
        Assert.True(context.HasOutcome("Success"));
        Assert.False(context.HasOutcome("Failure"));
        Assert.True(context.GetActivityOutput(() => activity.Result) as bool?);
        Assert.Contains(context.JournalData, x => x.Key == "ResultCode" && (ResultCode)x.Value == ResultCode.Success);
        Assert.Contains(context.JournalData, x => x.Key == "AddedEntry" && (string)x.Value == "cn=test,dc=example,dc=org");
        Assert.Contains(context.JournalData, x => x.Key == "AttributesCount" && (int)x.Value == 2);

        ldapConnectionMock.Verify(m => m.SendRequestAsync(
            It.Is<AddRequest>(r => r.DistinguishedName == "cn=test,dc=example,dc=org"
                && r.Attributes.Count == 2)));
    }

    [Theory]
    [InlineData(ResultCode.EntryAlreadyExists)]
    [InlineData(ResultCode.InsufficientAccessRights)]
    [InlineData(ResultCode.InvalidAttributeSyntax)]
    [InlineData(ResultCode.NamingViolation)]
    [InlineData(ResultCode.Other)]
    public async Task AddLdapEntry_ReturnsErrorWhenServerRespondsWithNonSuccess(ResultCode resultCode)
    {
        // Arrange
        var activity = new AddLdapEntry
        {
            ConnectionName = new("MyConnection"),
            EntryDn = new("cn=test,dc=example,dc=org"),
            Attributes = new([
                new DirectoryAttribute("cn", "test"),
                new DirectoryAttribute("sn", "user"),
            ]),
        };

        var ldapConnectionMock = new Mock<ILdapConnection>();
        ldapConnectionMock.Setup(m => m.SendRequestAsync(It.IsAny<AddRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestHelpers.CreateAddResponse(resultCode));

        var ldapConnectionFactoryMock = new Mock<ILdapConnectionFactory>();
        ldapConnectionFactoryMock.Setup(m => m.CreateConnection(It.IsAny<string?>()))
            .Returns(ldapConnectionMock.Object);

        // Act
        var fixture = new ActivityTestFixture(activity);
        fixture.Services.AddScoped(_ => ldapConnectionFactoryMock.Object);

        var context = await fixture.ExecuteAsync();

        // Assert
        Assert.Equal(ActivityStatus.Completed, context.Status);
        Assert.False(context.HasOutcome("Success"));
        Assert.True(context.HasOutcome("Failure"));
        Assert.False(context.GetActivityOutput(() => activity.Result) as bool?);
    }
}
