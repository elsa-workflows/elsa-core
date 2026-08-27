namespace Elsa.UserTasks.Persistence.ConformanceTests.Infrastructure;

/// <summary>
/// The persistence providers the conformance suite knows about, and whether this run can reach them.
///
/// A provider that cannot be reached is never silently omitted: it is reported by
/// <see cref="ConformanceCoverageTests"/> and every one of its tests is reported as skipped, with the
/// reason, by <see cref="ConformanceFactAttribute"/>. A provider that reads as "passed" must actually
/// have run.
/// </summary>
public static class ConformanceProviders
{
    public const string InMemory = "InMemory";
    public const string Sqlite = "EFCore.Sqlite";
    public const string SqlServer = "EFCore.SqlServer";
    public const string PostgreSql = "EFCore.PostgreSql";
    public const string Oracle = "EFCore.Oracle";
    public const string MySql = "EFCore.MySql";
    public const string VNext = "VNext.Sqlite";

    /// <summary>Every provider, in report order. Availability is resolved once per test run.</summary>
    public static IReadOnlyList<ConformanceProvider> All { get; } =
    [
        Always(InMemory, "Elsa.UserTasks in-process stores"),
        Always(Sqlite, "Elsa.UserTasks.Persistence.EFCore over SQLite"),
        Always(VNext, "Elsa.UserTasks.Persistence.VNext over the SQLite document store"),
        Gated(SqlServer, "Elsa.UserTasks.Persistence.EFCore over SQL Server", "ELSA_USERTASKS_TEST_SQLSERVER"),
        Gated(PostgreSql, "Elsa.UserTasks.Persistence.EFCore over PostgreSQL", "ELSA_USERTASKS_TEST_POSTGRES"),
        Gated(Oracle, "Elsa.UserTasks.Persistence.EFCore over Oracle", "ELSA_USERTASKS_TEST_ORACLE"),
        Blocked(MySql, "Elsa.UserTasks.Persistence.EFCore over MySQL",
            "Pomelo.EntityFrameworkCore.MySql 9.0.0 caps Microsoft.EntityFrameworkCore.Relational at 9.0.x while this " +
            "repository targets 10.0.9, so the module cannot be referenced from a test project at all (NU1107). " +
            "This provider is uncovered until the Pomelo pin moves to an EF Core 10 release.")
    ];

    public static ConformanceProvider Get(string name) =>
        All.FirstOrDefault(x => x.Name == name) ?? throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown conformance provider.");

    private static ConformanceProvider Always(string name, string description) => new(name, description, null, null);

    private static ConformanceProvider Gated(string name, string description, string variable) => new(
        name, description, variable,
        Environment.GetEnvironmentVariable(variable) is { Length: > 0 }
            ? null
            : $"{description} is not covered by this run: set {variable} to a connection string to include it.");

    private static ConformanceProvider Blocked(string name, string description, string reason) => new(name, description, null, reason);
}

/// <param name="Name">The stable provider key used by <see cref="ConformanceProviderAttribute"/>.</param>
/// <param name="Description">Human-readable description used in the coverage report.</param>
/// <param name="ConnectionStringVariable">The environment variable carrying the connection string, when gated.</param>
/// <param name="SkipReason">Null when the provider runs; otherwise the reason it does not.</param>
public sealed record ConformanceProvider(string Name, string Description, string? ConnectionStringVariable, string? SkipReason)
{
    public bool IsAvailable => SkipReason is null;

    public string ConnectionString => ConnectionStringVariable is null
        ? throw new InvalidOperationException($"Provider '{Name}' is not configured by a connection string.")
        : Environment.GetEnvironmentVariable(ConnectionStringVariable)
          ?? throw new InvalidOperationException($"Provider '{Name}' requires {ConnectionStringVariable} to be set.");
}
