using System.DirectoryServices.Protocols;
using Elsa.Extensions;
using Elsa.Ldap.Activities;
using Elsa.Ldap.Contracts;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Elsa.Ldap.UnitTests.Activities;

public class MoveLdapEntryTests
{
    [Fact]
    public async Task MoveLdapEntry_UpdatesEntry_ReturnsSuccessWhenServerRespondsWithSuccess()
    {
        // Arrange
        var activity = new MoveLdapEntry
        {
            ConnectionName = new("MyConnection"),
            EntryDn = new("cn=test,dc=example,dc=org"),
            NewParentDn = new("ou=users,dc=example,dc=org"),
            NewCn = new("test2"),
        };

        var ldapConnectionMock = new Mock<ILdapConnection>();
        ldapConnectionMock.Setup(m => m.SendRequestAsync(It.IsAny<ModifyDNRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestHelpers.CreateModifyDNResponse(ResultCode.Success));

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
        Assert.Contains(context.JournalData, x => x.Key == "ModifiedEntry" && (string)x.Value == "cn=test,dc=example,dc=org");

        ldapConnectionMock.Verify(m => m.SendRequestAsync(
            It.Is<ModifyDNRequest>(r => r.DistinguishedName == "cn=test,dc=example,dc=org"
                && r.NewParentDistinguishedName == "ou=users,dc=example,dc=org"
                && r.NewName == "test2")));
    }

    [Theory]
    [InlineData(ResultCode.EntryAlreadyExists)]
    [InlineData(ResultCode.InsufficientAccessRights)]
    [InlineData(ResultCode.InvalidAttributeSyntax)]
    [InlineData(ResultCode.NamingViolation)]
    [InlineData(ResultCode.Other)]
    public async Task MoveLdapEntry_ReturnsErrorWhenServerRespondsWithNonSuccess(ResultCode resultCode)
    {
        // Arrange
        var activity = new MoveLdapEntry
        {
            ConnectionName = new("MyConnection"),
            EntryDn = new("cn=test,dc=example,dc=org"),
            NewParentDn = new("ou=users,dc=example,dc=org"),
            NewCn = new("test2"),
        };

        var ldapConnectionMock = new Mock<ILdapConnection>();
        ldapConnectionMock.Setup(m => m.SendRequestAsync(It.IsAny<ModifyDNRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestHelpers.CreateModifyDNResponse(resultCode));

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
