using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.UnitTests;

public sealed class DbExceptionClassifierTests
{
    [Test]
    [Arguments(19, 1555, true)]
    [Arguments(19, 2067, true)]
    [Arguments(19, 19, false)]
    [Arguments(19, 0, false)]
    public async Task IsDuplicateKey_WhenSqliteExtendedCodeExists_UsesOnlyExtendedDuplicateCodes(int baseCode, int extendedCode, bool expected)
    {
        var exception = new SqliteExceptionWithExtendedCode(baseCode, extendedCode);

        await Assert.That(DbExceptionClassifier.IsDuplicateKey(exception)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(19, true)]
    [Arguments(1, false)]
    public async Task IsDuplicateKey_WhenSqliteExtendedCodeIsUnavailable_FallsBackToBaseCode(int baseCode, bool expected)
    {
        var exception = new SqliteExceptionWithoutExtendedCode(baseCode);

        await Assert.That(DbExceptionClassifier.IsDuplicateKey(exception)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(1205, true)]
    [Arguments(1213, true)]
    [Arguments(1062, false)]
    public async Task IsTransient_WhenMySqlProviderUsesInnoDbLockErrorCodes(int number, bool expected)
    {
        var exception = new MySqlConnector.MySqlException(number);

        await Assert.That(DbExceptionClassifier.IsTransient("Pomelo.EntityFrameworkCore.MySql", exception)).IsEqualTo(expected);
    }

    [Test]
    public async Task IsTransient_WhenMySqlProviderExceptionOnlyHasAnUnrelatedNumberProperty_ReturnsFalse()
    {
        var exception = new UnrelatedNumberException(1213);

        await Assert.That(DbExceptionClassifier.IsTransient("Pomelo.EntityFrameworkCore.MySql", exception)).IsFalse();
    }

    [Test]
    public async Task IsTransient_WhenMySqlDeadlockIsWrappedByDbUpdateException_ReturnsTrue()
    {
        var exception = new DbUpdateException("Write failed", new MySqlConnector.MySqlException(1213));

        await Assert.That(DbExceptionClassifier.IsTransient("Pomelo.EntityFrameworkCore.MySql", exception)).IsTrue();
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

    private sealed class UnrelatedNumberException(int number) : Exception
    {
        public int Number { get; } = number;
    }
}
