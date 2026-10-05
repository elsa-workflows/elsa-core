using Elsa.Persistence.Dapper.Dialects;
using Elsa.Persistence.Dapper.Extensions;
using Elsa.Persistence.Dapper.Models;

namespace Elsa.Dapper.UnitTests;

public class ParameterizedQueryBuilderExtensionsTests
{
    [Fact(DisplayName = "LessThan appends an exclusive comparison and binds @{field}")]
    public void LessThan_WithValue_AppendsExclusiveComparison()
    {
        var cutoff = new DateTimeOffset(2026, 1, 1, 12, 5, 0, TimeSpan.Zero);
        var query = new ParameterizedQuery(new SqliteDialect())
            .From("WorkflowInstances")
            .LessThan("UpdatedAt", cutoff);

        var sql = query.Sql.ToString();

        Assert.Contains("and UpdatedAt < @UpdatedAt", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("<=", sql, StringComparison.Ordinal);
        Assert.Equal(cutoff, query.Parameters.Get<DateTimeOffset>("UpdatedAt"));
    }

    [Fact(DisplayName = "LessThan is a no-op when the value is null")]
    public void LessThan_WithNullValue_PreservesOtherFilters()
    {
        var query = new ParameterizedQuery(new SqliteDialect())
            .From("WorkflowInstances")
            .Is("IsExecuting", true)
            .LessThan("UpdatedAt", null);

        var sql = query.Sql.ToString();

        Assert.Contains("and IsExecuting = @IsExecuting", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("UpdatedAt", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("UpdatedAt", query.Parameters.ParameterNames);
        Assert.True(query.Parameters.Get<bool>("IsExecuting"));
    }

    [Fact(DisplayName = "StartsWith binds @{field}StartsWith so it does not collide with Is(@{field})")]
    public void StartsWith_BindsFieldPrefixedParameter()
    {
        var query = new ParameterizedQuery(new SqliteDialect())
            .From("KeyValues")
            .StartsWith("Id", true, "app:");

        var sql = query.Sql.ToString();

        Assert.Contains("and Id like @IdStartsWith", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("@SearchTermLike", sql, StringComparison.Ordinal);
        Assert.Equal("app:%", query.Parameters.Get<string>("IdStartsWith"));
        Assert.DoesNotContain("Id", query.Parameters.ParameterNames.Where(name => name == "Id"));
    }

    [Fact(DisplayName = "IsNullOrEmpty matches NULL or empty string for the default tenant")]
    public void IsNullOrEmpty_MatchesNullOrEmptyTenantId()
    {
        var query = new ParameterizedQuery(new SqliteDialect())
            .From("KeyValues")
            .IsNullOrEmpty("TenantId");

        Assert.Contains("and (TenantId is null or TenantId = '')", query.Sql.ToString(), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "StartsWith quotes the identifier on PostgreSQL and binds @{field}StartsWith")]
    public void StartsWith_PostgreSql_QuotesIdentifierAndBindsFieldPrefixedParameter()
    {
        var query = new ParameterizedQuery(new PostgreSqlDialect())
            .From("KeyValues")
            .StartsWith("Id", true, "app:");

        var sql = query.Sql.ToString();

        Assert.Contains("and \"Id\" like @IdStartsWith", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("@SearchTermLike", sql, StringComparison.Ordinal);
        Assert.Equal("app:%", query.Parameters.Get<string>("IdStartsWith"));
    }
}
