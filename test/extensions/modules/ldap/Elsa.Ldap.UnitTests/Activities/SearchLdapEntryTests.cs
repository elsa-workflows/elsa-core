using System.DirectoryServices.Protocols;
using Elsa.Extensions;
using Elsa.Ldap.Activities;
using Elsa.Ldap.Contracts;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Elsa.Ldap.UnitTests.Activities;

public class SearchLdapEntryTests
{
    [Fact]
    public async Task SearchLdapEntry_WhenEntryExists_FindsAndReturnsEntry()
    {
        // Arrange
        var activity = new SearchLdapEntry
        {
            ConnectionName = new("MyConnection"),
            BaseDn = new("dc=example,dc=org"),
            Filter = new("(cn=test)"),
            Scope = new(SearchScope.Subtree),
            Attributes = new(["cn", "sn"]),
        };

        var ldapConnectionMock = new Mock<ILdapConnection>();
        ldapConnectionMock.Setup(m => m.SendRequestAsync(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestHelpers.CreateSearchResponse(
                TestHelpers.CreateSearchResultEntry("cn=test,dc=example,dc=org",
                    new DirectoryAttribute("cn", "test"),
                    new DirectoryAttribute("sn", "user")
                )
            ));

        var ldapConnectionFactoryMock = new Mock<ILdapConnectionFactory>();
        ldapConnectionFactoryMock.Setup(m => m.CreateConnection(It.IsAny<string?>()))
            .Returns(ldapConnectionMock.Object);

        // Act
        var fixture = new ActivityTestFixture(activity);
        fixture.Services.AddScoped(_ => ldapConnectionFactoryMock.Object);

        var context = await fixture.ExecuteAsync();

        // Assert
        Assert.Equal(ActivityStatus.Completed, context.Status);
        Assert.True(context.HasOutcome("Found"));
        Assert.False(context.HasOutcome("Not Found"));
        Assert.NotNull(context.GetActivityOutput(() => activity.Result));
        Assert.NotNull(context.GetActivityOutput(() => activity.SearchResult));
        Assert.Contains(context.JournalData, x => x.Key == "ResultCode" && (ResultCode)x.Value == ResultCode.Success);
        Assert.Contains(context.JournalData, x => x.Key == "MatchFound" && (bool)x.Value == true);

        ldapConnectionMock.Verify(m => m.SendRequestAsync(
            It.Is<SearchRequest>(r => r.DistinguishedName == "dc=example,dc=org"
                && (string)r.Filter == "(cn=test)"
                && r.Scope == SearchScope.Subtree
                && r.Attributes.Contains("cn")
                && r.Attributes.Contains("sn"))));
    }

    [Fact]
    public async Task SearchLdapEntry_WhenEntryDoesNotExists_ReturnsNoResult()
    {
        // Arrange
        var activity = new SearchLdapEntry
        {
            ConnectionName = new("MyConnection"),
            BaseDn = new("dc=example,dc=org"),
            Filter = new("(cn=test)"),
            Scope = new(SearchScope.Subtree),
            Attributes = new(["cn", "sn"]),
        };

        var ldapConnectionMock = new Mock<ILdapConnection>();
        ldapConnectionMock.Setup(m => m.SendRequestAsync(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestHelpers.CreateSearchResponse());

        var ldapConnectionFactoryMock = new Mock<ILdapConnectionFactory>();
        ldapConnectionFactoryMock.Setup(m => m.CreateConnection(It.IsAny<string?>()))
            .Returns(ldapConnectionMock.Object);

        // Act
        var fixture = new ActivityTestFixture(activity);
        fixture.Services.AddScoped(_ => ldapConnectionFactoryMock.Object);

        var context = await fixture.ExecuteAsync();

        // Assert
        Assert.Equal(ActivityStatus.Completed, context.Status);
        Assert.False(context.HasOutcome("Found"));
        Assert.True(context.HasOutcome("Not Found"));
        Assert.Null(context.GetActivityOutput(() => activity.Result));
        Assert.Null(context.GetActivityOutput(() => activity.SearchResult));
        Assert.Contains(context.JournalData, x => x.Key == "ResultCode" && (ResultCode)x.Value == ResultCode.Success);
        Assert.Contains(context.JournalData, x => x.Key == "MatchFound" && (bool)x.Value == false);

        ldapConnectionMock.Verify(m => m.SendRequestAsync(
            It.Is<SearchRequest>(r => r.DistinguishedName == "dc=example,dc=org"
                && (string)r.Filter == "(cn=test)"
                && r.Scope == SearchScope.Subtree
                && r.Attributes.Contains("cn")
                && r.Attributes.Contains("sn"))));
    }
}
