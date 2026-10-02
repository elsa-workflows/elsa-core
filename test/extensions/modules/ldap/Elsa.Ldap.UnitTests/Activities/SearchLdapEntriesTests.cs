using System.DirectoryServices.Protocols;
using Elsa.Extensions;
using Elsa.Ldap.Activities;
using Elsa.Ldap.Contracts;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Elsa.Ldap.UnitTests.Activities;

public class SearchLdapEntriesTests
{
    [Fact]
    public async Task SearchLdapEntries_WhenNoEntriesFound_ReturnsNoEntriesFoundOutcome()
    {
        // Arrange
        var activity = new SearchLdapEntries
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
        ldapConnectionFactoryMock.Setup(f => f.CreateConnection(It.IsAny<string?>()))
            .Returns(ldapConnectionMock.Object);

        // Act
        var fixture = new ActivityTestFixture(activity);
        fixture.Services.AddScoped(_ => ldapConnectionFactoryMock.Object);

        var context = await fixture.ExecuteAsync();

        // Assert
        Assert.Equal(ActivityStatus.Completed, context.Status);
        Assert.True(context.HasOutcome("No entries found"));
        Assert.False(context.HasOutcome("One entry found"));
        Assert.False(context.HasOutcome("Multiple entries found"));
        Assert.Contains(context.JournalData, x => x.Key == "ResultCode" && (ResultCode)x.Value == ResultCode.Success);
        Assert.Contains(context.JournalData, x => x.Key == "MatchCount" && (int)x.Value == 0);

        ldapConnectionMock.Verify(m => m.SendRequestAsync(
            It.Is<SearchRequest>(r => r.DistinguishedName == "dc=example,dc=org"
                && (string)r.Filter == "(cn=test)"
                && r.Scope == SearchScope.Subtree
                && r.Attributes.Contains("cn")
                && r.Attributes.Contains("sn"))));
    }

    [Fact]
    public async Task SearchLdapEntries_WhenOneEntryFound_ReturnsOneEntryFoundOutcome()
    {
        // Arrange
        var activity = new SearchLdapEntries
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
        ldapConnectionFactoryMock.Setup(f => f.CreateConnection(It.IsAny<string?>()))
            .Returns(ldapConnectionMock.Object);

        // Act
        var fixture = new ActivityTestFixture(activity);
        fixture.Services.AddScoped(_ => ldapConnectionFactoryMock.Object);

        var context = await fixture.ExecuteAsync();

        // Assert
        Assert.Equal(ActivityStatus.Completed, context.Status);
        Assert.False(context.HasOutcome("No entries found"));
        Assert.True(context.HasOutcome("One entry found"));
        Assert.False(context.HasOutcome("Multiple entries found"));
        Assert.NotNull(context.GetActivityOutput(() => activity.Result));
        Assert.NotNull(context.GetActivityOutput(() => activity.SearchResults));
        Assert.Contains(context.JournalData, x => x.Key == "ResultCode" && (ResultCode)x.Value == ResultCode.Success);
        Assert.Contains(context.JournalData, x => x.Key == "MatchCount" && (int)x.Value == 1);

        ldapConnectionMock.Verify(m => m.SendRequestAsync(
            It.Is<SearchRequest>(r => r.DistinguishedName == "dc=example,dc=org"
                && (string)r.Filter == "(cn=test)"
                && r.Scope == SearchScope.Subtree
                && r.Attributes.Contains("cn")
                && r.Attributes.Contains("sn"))));
    }

    [Fact]
    public async Task SearchLdapEntries_WhenMultipleEntriesFound_ReturnsMultipleEntriesFoundOutcome()
    {
        // Arrange
        var activity = new SearchLdapEntries
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
                TestHelpers.CreateSearchResultEntry("cn=test1,dc=example,dc=org",
                    new DirectoryAttribute("cn", "test1"),
                    new DirectoryAttribute("sn", "user1")
                ),
                TestHelpers.CreateSearchResultEntry("cn=test2,dc=example,dc=org",
                    new DirectoryAttribute("cn", "test2"),
                    new DirectoryAttribute("sn", "user2")
                )
            ));

        var ldapConnectionFactoryMock = new Mock<ILdapConnectionFactory>();
        ldapConnectionFactoryMock.Setup(f => f.CreateConnection(It.IsAny<string?>()))
            .Returns(ldapConnectionMock.Object);

        // Act
        var fixture = new ActivityTestFixture(activity);
        fixture.Services.AddScoped(_ => ldapConnectionFactoryMock.Object);

        var context = await fixture.ExecuteAsync();

        // Assert
        Assert.Equal(ActivityStatus.Completed, context.Status);
        Assert.False(context.HasOutcome("No entries found"));
        Assert.False(context.HasOutcome("One entry found"));
        Assert.True(context.HasOutcome("Multiple entries found"));
        Assert.NotNull(context.GetActivityOutput(() => activity.Result));
        Assert.NotNull(context.GetActivityOutput(() => activity.SearchResults));
        Assert.Contains(context.JournalData, x => x.Key == "ResultCode" && (ResultCode)x.Value == ResultCode.Success);
        Assert.Contains(context.JournalData, x => x.Key == "MatchCount" && (int)x.Value == 2);

        ldapConnectionMock.Verify(m => m.SendRequestAsync(
            It.Is<SearchRequest>(r => r.DistinguishedName == "dc=example,dc=org"
                && (string)r.Filter == "(cn=test)"
                && r.Scope == SearchScope.Subtree
                && r.Attributes.Contains("cn")
                && r.Attributes.Contains("sn"))));
    }
}
