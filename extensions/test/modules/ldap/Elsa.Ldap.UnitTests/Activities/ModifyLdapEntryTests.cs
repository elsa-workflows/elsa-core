using System.DirectoryServices.Protocols;
using Elsa.Extensions;
using Elsa.Ldap.Activities;
using Elsa.Ldap.Contracts;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Elsa.Ldap.UnitTests.Activities;

public class ModifyLdapEntryTests
{
    [Fact]
    public async Task ModifyLdapEntry_UpdatesEntry_ReturnsSuccessWhenServerRespondsWithSuccess()
    {
        // Arrange
        var modification1 = new DirectoryAttributeModification
        {
            Name = "cn",
            Operation = DirectoryAttributeOperation.Replace,
        };
        modification1.Add("test2");

        var modification2 = new DirectoryAttributeModification
        {
            Name = "sn",
            Operation = DirectoryAttributeOperation.Replace,
        };
        modification2.Add("user2");

        var activity = new ModifyLdapEntry
        {
            ConnectionName = new("MyConnection"),
            EntryDn = new("cn=test,dc=example,dc=org"),
            Modifications = new([modification1, modification2]),
        };

        var ldapConnectionMock = new Mock<ILdapConnection>();
        ldapConnectionMock.Setup(m => m.SendRequestAsync(It.IsAny<ModifyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestHelpers.CreateModifyResponse(ResultCode.Success));

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
        Assert.Contains(context.JournalData, x => x.Key == "ModificationCount" && (int)x.Value == 2);

        ldapConnectionMock.Verify(m => m.SendRequestAsync(
            It.Is<ModifyRequest>(r => r.DistinguishedName == "cn=test,dc=example,dc=org"
                && r.Modifications.Count == 2)));
    }

    [Theory]
    [InlineData(ResultCode.EntryAlreadyExists)]
    [InlineData(ResultCode.InsufficientAccessRights)]
    [InlineData(ResultCode.InvalidAttributeSyntax)]
    [InlineData(ResultCode.NamingViolation)]
    [InlineData(ResultCode.Other)]
    public async Task ModifyLdapEntry_ReturnsErrorWhenServerRespondsWithNonSuccess(ResultCode resultCode)
    {
        // Arrange
        var modification1 = new DirectoryAttributeModification
        {
            Name = "cn",
            Operation = DirectoryAttributeOperation.Replace,
        };
        modification1.Add("test2");

        var modification2 = new DirectoryAttributeModification
        {
            Name = "sn",
            Operation = DirectoryAttributeOperation.Replace,
        };
        modification2.Add("user2");

        var activity = new ModifyLdapEntry
        {
            ConnectionName = new("MyConnection"),
            EntryDn = new("cn=test,dc=example,dc=org"),
            Modifications = new([modification1, modification2]),
        };

        var ldapConnectionMock = new Mock<ILdapConnection>();
        ldapConnectionMock.Setup(m => m.SendRequestAsync(It.IsAny<ModifyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestHelpers.CreateModifyResponse(resultCode));

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
