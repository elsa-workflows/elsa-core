using System.DirectoryServices.Protocols;
using Elsa.Extensions;
using Elsa.Ldap.Activities;
using Elsa.Ldap.Contracts;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Elsa.Ldap.UnitTests.Activities;

public class DeleteLdapEntryTests
{
    [Fact]
    public async Task DeleteLdapEntry_DeletesEntry_ReturnsSuccessWhenServerRespondsWithSuccess()
    {
        // Arrange
        var activity = new DeleteLdapEntry
        {
            ConnectionName = new("MyConnection"),
            EntryDn = new("cn=test,dc=example,dc=org"),
        };

        var ldapConnectionMock = new Mock<ILdapConnection>();
        ldapConnectionMock.Setup(m => m.SendRequestAsync(It.IsAny<DeleteRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestHelpers.CreateDeleteResponse(ResultCode.Success));

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
        Assert.Contains(context.JournalData, x => x.Key == "DeletedEntry" && (string)x.Value == "cn=test,dc=example,dc=org");

        ldapConnectionMock.Verify(m => m.SendRequestAsync(
            It.Is<DeleteRequest>(r => r.DistinguishedName == "cn=test,dc=example,dc=org")));
    }

    [Theory]
    [InlineData(ResultCode.EntryAlreadyExists)]
    [InlineData(ResultCode.InsufficientAccessRights)]
    [InlineData(ResultCode.InvalidAttributeSyntax)]
    [InlineData(ResultCode.NamingViolation)]
    [InlineData(ResultCode.Other)]
    public async Task DeleteLdapEntry_ReturnsErrorWhenServerRespondsWithNonSuccess(ResultCode resultCode)
    {
        // Arrange
        var activity = new DeleteLdapEntry
        {
            ConnectionName = new("MyConnection"),
            EntryDn = new("cn=test,dc=example,dc=org"),
        };

        var ldapConnectionMock = new Mock<ILdapConnection>();
        ldapConnectionMock.Setup(m => m.SendRequestAsync(It.IsAny<DeleteRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestHelpers.CreateDeleteResponse(resultCode));

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
