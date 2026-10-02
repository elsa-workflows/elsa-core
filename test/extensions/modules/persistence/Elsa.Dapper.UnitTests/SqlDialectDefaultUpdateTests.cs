using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Persistence.Dapper.Contracts;

namespace Elsa.Dapper.UnitTests;

public class SqlDialectDefaultUpdateTests
{
    [Fact(DisplayName = "Direct ISqlDialect implementers inherit the default Update(table, fields) SQL")]
    public void Update_WithoutCustomImplementation_ReturnsWhereAlwaysTrueSql()
    {
        // Arrange
        var dialect = new StubSqlDialect();

        // Act
        var sql = ((ISqlDialect)dialect).Update("T", ["A", "B"]);

        // Assert
        Assert.Equal("UPDATE T SET A = @A, B = @B WHERE 1=1", sql);
    }

    /// <summary>
    /// Implements <see cref="ISqlDialect"/> directly without the new Update(table, fields) overload.
    /// </summary>
    private sealed class StubSqlDialect : ISqlDialect
    {
        public string From(string table) => throw new NotImplementedException();
        public string From(string table, params string[] fields) => throw new NotImplementedException();
        public string Delete(string table) => throw new NotImplementedException();
        public string Count(string table) => throw new NotImplementedException();
        public string Count(string fieldExpression, string table) => throw new NotImplementedException();
        public string And(string field) => throw new NotImplementedException();
        public string AndNot(string field) => throw new NotImplementedException();
        public string And(string field, string[] fieldParamNames) => throw new NotImplementedException();
        public string AndNot(string field, string[] fieldParamNames) => throw new NotImplementedException();
        public string IsNull(string field) => throw new NotImplementedException();
        public string IsNotNull(string field) => throw new NotImplementedException();
        public string OrderBy(string field, OrderDirection direction) => throw new NotImplementedException();
        public string Skip(int count) => throw new NotImplementedException();
        public string Take(int count) => throw new NotImplementedException();
        public string Page(PageArgs pageArgs) => throw new NotImplementedException();
        public string Insert(string table, string[] fields, Func<string, string>? getParamName = null) => throw new NotImplementedException();
        public string Update(string table, string primaryKeyField, string[] fields, Func<string, string>? getParamName = null) => throw new NotImplementedException();
        public string Upsert(string table, string primaryKeyField, string[] fields, Func<string, string>? getParamName = null) => throw new NotImplementedException();
    }
}
