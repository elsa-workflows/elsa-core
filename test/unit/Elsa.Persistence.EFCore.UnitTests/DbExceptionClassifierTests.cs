namespace Elsa.Persistence.EFCore.UnitTests;

public sealed class DbExceptionClassifierTests
{
    [Theory]
    [InlineData(19, 1555, true)]
    [InlineData(19, 2067, true)]
    [InlineData(19, 19, false)]
    [InlineData(19, 0, false)]
    public void IsDuplicateKey_WhenSqliteExtendedCodeExists_UsesOnlyExtendedDuplicateCodes(int baseCode, int extendedCode, bool expected)
    {
        var exception = new SqliteExceptionWithExtendedCode(baseCode, extendedCode);

        Assert.Equal(expected, DbExceptionClassifier.IsDuplicateKey(exception));
    }

    [Theory]
    [InlineData(19, true)]
    [InlineData(1, false)]
    public void IsDuplicateKey_WhenSqliteExtendedCodeIsUnavailable_FallsBackToBaseCode(int baseCode, bool expected)
    {
        var exception = new SqliteExceptionWithoutExtendedCode(baseCode);

        Assert.Equal(expected, DbExceptionClassifier.IsDuplicateKey(exception));
    }

    private sealed class SqliteExceptionWithExtendedCode(int sqliteErrorCode, int sqliteExtendedErrorCode) : Exception
    {
        public int SqliteErrorCode { get; } = sqliteErrorCode;
        public int SqliteExtendedErrorCode { get; } = sqliteExtendedErrorCode;
    }

    private sealed class SqliteExceptionWithoutExtendedCode(int sqliteErrorCode) : Exception
    {
        public int SqliteErrorCode { get; } = sqliteErrorCode;
    }
}
