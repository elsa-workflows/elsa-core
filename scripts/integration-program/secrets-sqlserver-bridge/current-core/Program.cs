using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.EntityHandlers;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Models;
using Elsa.Secrets.Options;
using Elsa.Secrets.Persistence.EFCore;
using Elsa.Secrets.Persistence.EFCore.Repositories;
using Elsa.Secrets.Persistence.EFCore.SqlServer;
using Elsa.Secrets.Services;
using Elsa.Secrets.Stores;
using Elsa.Tenants.Options;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;
using Secret = Elsa.Secrets.Models.Secret;

internal static class Program
{
    private const string Phase = "current-core-sqlserver-bridge";
    private const string DataProtectionApplicationName = "Elsa-Secrets-Bridge-Contract-Synthetic";
    private const string DataProtectionPurpose = "Elsa.Secrets.Encryption";
    private const string SidecarTable = "ElsaSecretsLegacyV381";
    private const string TargetCoreCommit = "0b20ab54a60a61b025d51a268f5b747e3a3c4860";
    private static string DiagnosticStage = "startup";
    private static readonly byte[] CoreKey = Enumerable.Range(1, 32).Select(value => (byte)value).ToArray();
    private static readonly byte[] WrongCoreKey = Enumerable.Range(33, 32).Select(value => (byte)value).ToArray();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<int> Main(string[] args)
    {
        if (args.Length != 7)
        {
            Console.Error.WriteLine("Usage: SqlServerBridgeRunner <source-connection> <target-connection> <old-key-ring> <wrong-key-ring> <missing-key-ring> <tenant-map.json> <success|id-collision|fail-after-core-save>");
            return 64;
        }

        try
        {
            var result = await RunAsync(args);
            Console.WriteLine(JsonSerializer.Serialize(result));
            return 0;
        }
        catch (BridgeRejectedException error)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                phase = Phase,
                result = "rejected",
                targetCoreSourceCommit = TargetCoreCommit,
                rejectionCode = error.Code,
                conversionWritesUnchanged = error.ConversionWritesUnchanged,
                originalDestinationUnchanged = error.OriginalDestinationUnchanged,
                plaintextPrinted = false,
                keysPrinted = false,
                ciphertextPrinted = false
            }));
            return error.ConversionWritesUnchanged ? 0 : 1;
        }
        catch (Exception error) when (
            error is not (OutOfMemoryException
                or StackOverflowException
                or AccessViolationException
                or AppDomainUnloadedException
                or BadImageFormatException))
        {
            // Keep unexpected failure output limited to a type name; fixture data and provider details stay private.
            var sqlState = error is SqlException sqlError ? $" SQL error number={sqlError.Number}" : string.Empty;
            Console.Error.WriteLine($"Bridge fixture failed at {DiagnosticStage}: {error.GetType().Name}{sqlState}");
            return 1;
        }
    }

    private static async Task<object> RunAsync(string[] args)
    {
        var sourceConnectionString = args[0];
        var targetConnectionString = args[1];
        var oldKeyRingPath = Path.GetFullPath(args[2]);
        var wrongKeyRingPath = Path.GetFullPath(args[3]);
        var missingKeyRingPath = Path.GetFullPath(args[4]);
        var tenantMapPath = Path.GetFullPath(args[5]);
        var scenario = args[6];

        DiagnosticStage = "connection-validation";
        var sourceConnection = new SqlConnectionStringBuilder(sourceConnectionString);
        var targetConnection = new SqlConnectionStringBuilder(targetConnectionString);
        if (string.Equals(sourceConnection.DataSource, targetConnection.DataSource, StringComparison.OrdinalIgnoreCase)
            && string.Equals(sourceConnection.InitialCatalog, targetConnection.InitialCatalog, StringComparison.Ordinal))
        {
            throw new BridgeRejectedException("InvalidDatabasePaths");
        }

        DiagnosticStage = "target-preflight";
        if (await TargetSchemaHasObjectsAsync(targetConnectionString))
        {
            throw new BridgeRejectedException("TargetAlreadyExists");
        }

        DiagnosticStage = "source-schema-read";
        var tenantMap = await LoadTenantMapAsync(tenantMapPath);
        var source = await ReadSourceAsync(sourceConnectionString);
        DiagnosticStage = "source-preflight";
        var groups = Preflight(source, tenantMap, Array.Empty<ExistingTarget>());
        var oldProvider = CreateDataProtectionProvider(oldKeyRingPath);
        var oldProtector = oldProvider.CreateProtector(DataProtectionPurpose);
        var wrongOldProtector = CreateDataProtectionProvider(wrongKeyRingPath).CreateProtector(DataProtectionPurpose);
        var missingOldProtector = CreateDataProtectionProvider(missingKeyRingPath).CreateProtector(DataProtectionPurpose);
        var wrongContextProtector = CreateDataProtectionProvider(oldKeyRingPath, "Elsa-Secrets-Bridge-Contract-Wrong-Context").CreateProtector(DataProtectionPurpose);

        if (source.Any(row => CanUnprotect(wrongOldProtector, row.EncryptedValue))
            || source.Any(row => CanUnprotect(missingOldProtector, row.EncryptedValue))
            || source.Any(row => CanUnprotect(wrongContextProtector, row.EncryptedValue)))
        {
            throw new BridgeRejectedException("OldKeyRingIsolationFailed");
        }

        var conversionOldProtector = scenario switch
        {
            "wrong-old-key" => wrongOldProtector,
            "missing-old-key" => missingOldProtector,
            "wrong-data-protection-context" => wrongContextProtector,
            _ => oldProtector
        };
        DiagnosticStage = "old-key-decrypt";
        var plaintextByLegacyId = new Dictionary<string, string>(StringComparer.Ordinal);
        try
        {
            foreach (var row in source)
            {
                plaintextByLegacyId.Add(row.Id, conversionOldProtector.Unprotect(row.EncryptedValue));
            }
        }
        catch (CryptographicException)
        {
            throw new BridgeRejectedException("OldKeyUnavailable");
        }

        var coreProtector = new DefaultSecretValueProtector(Options.Create(new SecretsOptions { EncryptionKey = CoreKey }));
        var encryptedStore = new EncryptedSecretStore(coreProtector);
        if (scenario == "missing-core-key")
        {
            try
            {
                _ = new DefaultSecretValueProtector(Options.Create(new SecretsOptions())).Protect("synthetic probe");
                throw new BridgeRejectedException("MissingCoreKeyAccepted");
            }
            catch (InvalidOperationException)
            {
                throw new BridgeRejectedException("MissingCoreKey");
            }
        }

        if (scenario == "wrong-core-key")
        {
            var probe = new Secret { Id = "synthetic-core-key-probe", TypeName = SecretTypeNames.Text, StoreName = SecretStoreNames.Encrypted };
            var version = new SecretVersion { Version = 1, Payload = SecretPayload.FromValue("synthetic probe") };
            version.Payload = await encryptedStore.WriteAsync(probe, version, version.Payload);
            try
            {
                _ = await new EncryptedSecretStore(new DefaultSecretValueProtector(Options.Create(new SecretsOptions { EncryptionKey = WrongCoreKey })))
                    .ReadAsync(probe, version);
                throw new BridgeRejectedException("WrongCoreKeyAccepted");
            }
            catch (CryptographicException)
            {
                throw new BridgeRejectedException("WrongCoreKey");
            }
        }

        DiagnosticStage = "target-migrate";
        var tenantAccessor = new DefaultTenantAccessor();
        await using var services = CreateCurrentServices(targetConnectionString, tenantAccessor);
        await MigrateTargetAsync(services);

        if (scenario == "id-collision")
        {
            await SeedAggregateIdCollisionAsync(services);
        }

        DiagnosticStage = "target-snapshot";
        var targetBefore = await SnapshotTargetAsync(services);
        try
        {
            DiagnosticStage = "target-preflight-existing";
            groups = Preflight(source, tenantMap, await ReadExistingTargetAsync(services));

            var converted = new List<Secret>();

            foreach (var group in groups)
            {
                var latest = group.Rows.Single(row => row.IsLatest);
                var secret = new Secret
                {
                    Id = group.Key,
                    TenantId = group.TargetTenantId,
                    Name = latest.Name,
                    DisplayName = latest.Name,
                    Description = latest.Description,
                    TypeName = SecretTypeNames.Text,
                    StoreName = SecretStoreNames.Encrypted,
                    Scope = latest.ScopeRaw,
                    Status = MapStatus(latest.Status),
                    CreatedAt = group.Rows.Min(row => row.CreatedAt),
                    UpdatedAt = group.Rows.Max(row => row.UpdatedAt),
                    Tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                };

                foreach (var row in group.Rows.OrderBy(row => row.Version))
                {
                    var version = new SecretVersion
                    {
                        Version = row.Version,
                        Status = MapStatus(row.Status),
                        CreatedAt = row.CreatedAt,
                        ExpiresAt = row.ExpiresAt,
                        Payload = SecretPayload.FromValue(plaintextByLegacyId[row.Id])
                    };
                    version.Payload = await encryptedStore.WriteAsync(secret, version, version.Payload);
                    secret.Versions.Add(version);
                }

                converted.Add(secret);
            }

            DiagnosticStage = "persist-conversion";
            await PersistConversionAsync(services, converted, source, scenario);

            DiagnosticStage = "verify-target-snapshot";
            var targetAfter = await SnapshotTargetAsync(services);
            DiagnosticStage = "verify-sidecar";
            var sidecarExact = await VerifySidecarAsync(services, source, converted);
            var expiryMappingExact = converted.SelectMany(secret => secret.Versions.Select(version =>
            {
                var sourceRow = source.Single(row => row.SecretId == secret.Id && row.Version == version.Version);
                return (sourceRow.ExpiresAt is null && version.ExpiresAt is null)
                    || (sourceRow.ExpiresAt is { } expiresAt && version.ExpiresAt is { } targetExpiry && expiresAt.EqualsExact(targetExpiry));
            })).All(exact => exact);
            var statusMappingExact = converted.All(secret =>
                secret.Status == MapStatus(source.Single(row => row.SecretId == secret.Id && row.IsLatest).Status)
                && secret.Versions.All(version => version.Status == MapStatus(source.Single(row => row.SecretId == secret.Id && row.Version == version.Version).Status)));
            var tenantMappingExact = converted.All(secret =>
            {
                var sourceTenant = NormalizeLegacyTenant(source.First(row => row.SecretId == secret.Id).TenantIdRaw);
                var expectedTenant = sourceTenant == Tenant.DefaultTenantId ? Tenant.DefaultTenantId : tenantMap[sourceTenant];
                return secret.TenantId == expectedTenant;
            });
            DiagnosticStage = "verify-expiry-and-tenant";
            var repositoryIsolation = await VerifyTenantRepositoryAsync(services, tenantAccessor);
            DiagnosticStage = "verify-encryption";
            var persistedSecrets = await ReadAllTenantVisibleAsync(services, tenantAccessor);
            var encryption = await VerifyEncryptionAsync(persistedSecrets, source, plaintextByLegacyId, encryptedStore, coreProtector);
            var lifecycleOwnershipMarkersNotInvented = persistedSecrets.All(secret =>
                secret.ManagedOwnerId == null && secret.ManagedGenerationId == null);
            var sidecarCount = await ReadSidecarCountAsync(services);
            var targetMigrationIds = await ReadTargetMigrationIdsAsync(services);
            var legacyOwnerAuthorizationAdapterRequired = source.Any(row => !string.IsNullOrWhiteSpace(row.Owner));
            var legacyIdCompatibilityAdapterRequired = source.Any(row => !string.IsNullOrWhiteSpace(row.Id));
            var success = targetAfter.SecretIds.Count == 4
                && targetAfter.SecretIds.SequenceEqual(converted.Select(secret => secret.Id).Order(StringComparer.Ordinal), StringComparer.Ordinal)
                && persistedSecrets.Count == converted.Count
                && persistedSecrets.Sum(secret => secret.Versions.Count) == source.Count
                && sidecarCount == source.Count
                && sidecarExact
                && expiryMappingExact
                && statusMappingExact
                && tenantMappingExact
                && lifecycleOwnershipMarkersNotInvented
                && repositoryIsolation
                && encryption.RoundTrip
                && encryption.WrongCoreKeyRejected
                && encryption.MissingCoreKeyRejected
                && encryption.RawLegacyCiphertextRejected
                && legacyOwnerAuthorizationAdapterRequired
                && legacyIdCompatibilityAdapterRequired
                && targetMigrationIds.SequenceEqual(ExpectedTargetMigrationIds, StringComparer.Ordinal);

            if (!success)
            {
                throw new InvalidOperationException("Current-Core fixture assertions failed.");
            }

            return new
            {
                phase = Phase,
                result = "converted",
                targetCoreSourceCommit = TargetCoreCommit,
                targetMigrationIds,
                targetSnapshotSha256 = targetAfter.Digest,
                sourceRows = source.Count,
                sourceAggregateIds = source.Select(row => row.SecretId).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal),
                targetAggregates = converted.Count,
                targetVersions = converted.Sum(secret => secret.Versions.Count),
                persistedAggregates = persistedSecrets.Count,
                persistedVersions = persistedSecrets.Sum(secret => secret.Versions.Count),
                sidecarRows = sidecarCount,
                expiresAtMappingExact = expiryMappingExact,
                expiresInSidecarExact = sidecarExact,
                statusMappingExact,
                tenantMappingExact,
                sourceExpiresInValuesSqlTimeRepresentable = source.All(row => row.ExpiresInRaw == null || TimeSpan.Parse(row.ExpiresInRaw, CultureInfo.InvariantCulture) < TimeSpan.FromDays(1)),
                nativeTenantMappingVerified = tenantMappingExact,
                defaultTenantStoredAsEmpty = converted.Where(secret => secret.Id == "legacy-aggregate-default").All(secret => secret.TenantId == Tenant.DefaultTenantId),
                crossTenantReadIsolationVerified = repositoryIsolation,
                crossTenantSameNameVerified = true,
                crossTenantWriteIsolationTested = false,
                sidecarFieldValuesExact = sidecarExact,
                lifecycleOwnershipMarkersNotInvented,
                encryptedValuesRewritten = encryption.RoundTrip,
                rawLegacyCiphertextRejectedByCoreStore = encryption.RawLegacyCiphertextRejected,
                wrongCoreKeyRejected = encryption.WrongCoreKeyRejected,
                missingCoreKeyRejected = encryption.MissingCoreKeyRejected,
                wrongOldKeyRejected = true,
                missingOldKeyRejected = true,
                wrongDataProtectionContextRejected = true,
                legacyOwnerAuthorizationAdapterRequired,
                legacyIdCompatibilityAdapterRequired,
                cutoverAllowed = false,
                plaintextSha256 = source.Select(row => Hash(plaintextByLegacyId[row.Id])).Order(StringComparer.Ordinal).ToArray(),
                plaintextPrinted = false,
                keysPrinted = false,
                ciphertextPrinted = false
            };
        }
        catch (BridgeRejectedException error)
        {
            var targetAfter = await SnapshotTargetAsync(services);
            var unchanged = string.Equals(targetBefore.Digest, targetAfter.Digest, StringComparison.Ordinal);
            throw new BridgeRejectedException(error.Code, unchanged, originalDestinationUnchanged: false);
        }
    }

    private static readonly string[] ExpectedTargetMigrationIds =
    [
        "20260531141743_Initial",
        "20260825230253_SecretTenancy",
        "20260914120000_SecretDefaultTenantUniqueness",
        "20260923164247_ManagedSecretOwnership"
    ];

    private static async Task<bool> TargetSchemaHasObjectsAsync(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT CASE WHEN EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'Elsa') THEN 1 ELSE 0 END";
        return Convert.ToInt32(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture) != 0;
    }

    private static ServiceProvider CreateCurrentServices(string connectionString, DefaultTenantAccessor tenantAccessor)
    {
        var services = new ServiceCollection()
            .AddSingleton<ITenantAccessor>(tenantAccessor)
            .Configure<TenantsOptions>(options => options.IsEnabled = true)
            .AddScoped<IEntitySavingHandler, ApplyTenantId>()
            .AddScoped<IEntityModelCreatingHandler, SetTenantIdFilter>()
            .AddDbContextFactory<SecretsElsaDbContext>(builder => builder
                .EnableServiceProviderCaching(false)
                .UseElsaSqlServer(typeof(SecretsDbContextFactory).Assembly, connectionString))
            .Decorate<IDbContextFactory<SecretsElsaDbContext>, TenantAwareDbContextFactory<SecretsElsaDbContext>>()
            .AddSingleton<ISecretNameValidator, DefaultSecretNameValidator>()
            .AddScoped<Store<SecretsElsaDbContext, Secret>>()
            .AddScoped<EFCoreSecretRepository>()
            .BuildServiceProvider();
        return services;
    }

    private static async Task MigrateTargetAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SecretsElsaDbContext>>();
        await using var dbContext = await factory.CreateDbContextAsync();
        var migrator = dbContext.GetService<IMigrator>();
        foreach (var migrationId in ExpectedTargetMigrationIds)
        {
            DiagnosticStage = $"target-migration-{migrationId}";
            await migrator.MigrateAsync(migrationId);
        }
        var migrations = (await dbContext.Database.GetAppliedMigrationsAsync()).ToArray();
        if (!migrations.SequenceEqual(ExpectedTargetMigrationIds, StringComparer.Ordinal))
        {
            throw new BridgeRejectedException("UnexpectedTargetMigrationSet");
        }
    }

    private static async Task<Dictionary<string, string>> LoadTenantMapAsync(string path)
    {
        var map = JsonSerializer.Deserialize<Dictionary<string, string>>(await File.ReadAllTextAsync(path))
            ?? throw new BridgeRejectedException("InvalidTenantMap");
        if (map.Any(item => string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value)))
        {
            throw new BridgeRejectedException("InvalidTenantMap");
        }

        return map;
    }

    private static async Task<List<LegacyRow>> ReadSourceAsync(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var migrationIds = new List<string>();
        await using (var migrationCommand = connection.CreateCommand())
        {
            migrationCommand.CommandText = "SELECT [MigrationId] FROM [Elsa].[__EFMigrationsHistory] ORDER BY [MigrationId]";
            await using var migrationReader = await migrationCommand.ExecuteReaderAsync();
            while (await migrationReader.ReadAsync())
            {
                migrationIds.Add(migrationReader.GetString(0));
            }
        }

        if (!migrationIds.SequenceEqual(["20241011092820_V3_3"], StringComparer.Ordinal))
        {
            throw new BridgeRejectedException("UnknownSourceMigrationHistory");
        }

        var tables = new List<string>();
        await using (var tableCommand = connection.CreateCommand())
        {
            tableCommand.CommandText = "SELECT TABLE_NAME FROM information_schema.tables WHERE table_schema = 'Elsa' AND table_type = 'BASE TABLE' ORDER BY TABLE_NAME";
            await using var tableReader = await tableCommand.ExecuteReaderAsync();
            while (await tableReader.ReadAsync())
            {
                tables.Add(tableReader.GetString(0));
            }
        }

        var allowedTables = new HashSet<string>(["Secrets", "__EFMigrationsHistory"], StringComparer.Ordinal);
        if (!tables.Contains("Secrets", StringComparer.Ordinal)
            || !tables.Contains("__EFMigrationsHistory", StringComparer.Ordinal)
            || tables.Any(table => !allowedTables.Contains(table)))
        {
            throw new BridgeRejectedException("UnknownSourceSchema");
        }

        var columns = new List<string>();
        await using (var schemaCommand = connection.CreateCommand())
        {
            schemaCommand.CommandText = "SELECT COLUMN_NAME, DATA_TYPE FROM information_schema.columns WHERE table_schema = 'Elsa' AND table_name = 'Secrets' ORDER BY ordinal_position";
            await using var schemaReader = await schemaCommand.ExecuteReaderAsync();
            while (await schemaReader.ReadAsync())
            {
                columns.Add(schemaReader.GetString(0));
                if (schemaReader.GetString(0) == "ExpiresIn" && !string.Equals(schemaReader.GetString(1), "time", StringComparison.OrdinalIgnoreCase))
                    throw new BridgeRejectedException("UnknownSourceExpiryType");
            }
        }

        if (!columns.SequenceEqual(ExpectedSourceColumns, StringComparer.Ordinal))
        {
            throw new BridgeRejectedException("UnknownSourceSchema");
        }

        var rows = new List<LegacyRow>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT [Id], [SecretId], [Name], [Scope], [EncryptedValue], [Description], [Version], [IsLatest], [Status],
                       [ExpiresIn], [ExpiresAt], [LastAccessedAt], [TenantId], [CreatedAt], [UpdatedAt], [Owner]
                FROM [Elsa].[Secrets]
                ORDER BY [SecretId], [Version], [Id];
                """;
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var createdAtRaw = FormatTimestamp(reader.GetFieldValue<DateTimeOffset>(13));
                var updatedAtRaw = FormatTimestamp(reader.GetFieldValue<DateTimeOffset>(14));
                var expiresIn = reader.IsDBNull(9) ? null : reader.GetFieldValue<TimeSpan>(9).ToString("c", CultureInfo.InvariantCulture);
                var expiresAt = reader.IsDBNull(10) ? null : FormatTimestamp(reader.GetFieldValue<DateTimeOffset>(10));
                var lastAccessedAt = reader.IsDBNull(11) ? null : FormatTimestamp(reader.GetFieldValue<DateTimeOffset>(11));
                rows.Add(new LegacyRow(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    NullableString(reader, 3),
                    reader.GetString(4),
                    reader.GetString(5),
                    reader.GetInt32(6),
                    reader.GetBoolean(7),
                    reader.GetInt32(8),
                    expiresIn,
                    expiresAt,
                    lastAccessedAt,
                    NullableString(reader, 12),
                    createdAtRaw,
                    updatedAtRaw,
                    NullableString(reader, 15)));
            }
        }

        foreach (var row in rows)
        {
            row.CreatedAt = ParseOffset(row.CreatedAtRaw, "CreatedAt");
            row.UpdatedAt = ParseOffset(row.UpdatedAtRaw, "UpdatedAt");
            row.ExpiresAt = ParseOptionalOffset(row.ExpiresAtRaw, "ExpiresAt");
            row.LastAccessedAt = ParseOptionalOffset(row.LastAccessedAtRaw, "LastAccessedAt");
            if (!string.IsNullOrEmpty(row.ExpiresInRaw)
                && (!TimeSpan.TryParse(row.ExpiresInRaw, CultureInfo.InvariantCulture, out var expiresIn)
                    || expiresIn < TimeSpan.Zero || expiresIn >= TimeSpan.FromDays(1)))
            {
                throw new BridgeRejectedException("InvalidExpiresIn");
            }
        }

        return rows;
    }

    private static readonly string[] ExpectedSourceColumns =
    [
        "Id", "SecretId", "Name", "Scope", "EncryptedValue", "Description", "Version", "IsLatest", "Status",
        "ExpiresIn", "ExpiresAt", "LastAccessedAt", "TenantId", "CreatedAt", "UpdatedAt", "Owner"
    ];

    private static string? NullableString(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static string FormatTimestamp(DateTimeOffset value) => value.ToString("O", CultureInfo.InvariantCulture);

    private static DateTimeOffset ParseOffset(string raw, string field)
    {
        if (!HasOffset(raw) || !DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value))
        {
            throw new BridgeRejectedException($"Invalid{field}");
        }

        return value;
    }

    private static DateTimeOffset? ParseOptionalOffset(string? raw, string field) =>
        raw == null ? null : ParseOffset(raw, field);

    private static bool HasOffset(string raw) => Regex.IsMatch(raw, "(?:Z|[+-][0-9]{2}:[0-9]{2})$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static IReadOnlyList<ConversionGroup> Preflight(
        IReadOnlyList<LegacyRow> rows,
        IReadOnlyDictionary<string, string> tenantMap,
        IReadOnlyList<ExistingTarget> existing)
    {
        if (rows.Count == 0)
        {
            throw new BridgeRejectedException("EmptySource");
        }

        if (rows.GroupBy(row => row.Id, StringComparer.Ordinal).Any(group => group.Count() != 1))
        {
            throw new BridgeRejectedException("DuplicateLegacyRowId");
        }

        var validator = new DefaultSecretNameValidator();
        var result = new List<ConversionGroup>();
        var nameKeys = new HashSet<(string Tenant, string NormalizedName)>();
        foreach (var row in existing.Where(row => !string.IsNullOrWhiteSpace(row.NormalizedName)))
        {
            nameKeys.Add((row.TenantId ?? Tenant.DefaultTenantId, row.NormalizedName!));
        }
        var ids = existing.Select(row => row.Id).ToHashSet(StringComparer.Ordinal);

        foreach (var group in rows.GroupBy(row => row.SecretId, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(group.Key))
            {
                throw new BridgeRejectedException("InvalidAggregateId");
            }

            var versions = group.OrderBy(row => row.Version).ToArray();
            if (versions.Any(row => row.Version <= 0)
                || versions.GroupBy(row => row.Version).Any(items => items.Count() != 1))
            {
                throw new BridgeRejectedException("InvalidVersionSequence");
            }

            var latest = versions.Where(row => row.IsLatest).ToArray();
            if (latest.Length != 1 || latest[0].Version != versions[^1].Version)
            {
                throw new BridgeRejectedException("InvalidLatestMarker");
            }

            var tenantValues = versions.Select(row => NormalizeLegacyTenant(row.TenantIdRaw)).Distinct(StringComparer.Ordinal).ToArray();
            if (tenantValues.Length != 1)
            {
                throw new BridgeRejectedException("TenantChangesWithinAggregate");
            }

            var legacyTenantId = tenantValues[0];
            var targetTenantId = legacyTenantId == Tenant.DefaultTenantId
                ? Tenant.DefaultTenantId
                : tenantMap.TryGetValue(legacyTenantId, out var mappedTenant)
                    ? mappedTenant
                    : throw new BridgeRejectedException("UnmappedTenant");
            if (targetTenantId != Tenant.DefaultTenantId && !tenantMap.Values.Contains(targetTenantId, StringComparer.Ordinal))
            {
                throw new BridgeRejectedException("UnmappedTenant");
            }

            foreach (var row in versions)
            {
                if (!validator.IsValid(row.Name, out _))
                {
                    throw new BridgeRejectedException("InvalidSecretName");
                }

                _ = MapStatus(row.Status);
                if (string.IsNullOrWhiteSpace(row.EncryptedValue))
                {
                    throw new BridgeRejectedException("MissingEncryptedValue");
                }
            }

            var normalizedName = validator.Normalize(latest[0].Name);
            if (!nameKeys.Add((targetTenantId, normalizedName)))
            {
                throw new BridgeRejectedException("NormalizedNameCollision");
            }

            if (!ids.Add(group.Key))
            {
                throw new BridgeRejectedException("AggregateIdCollision");
            }

            result.Add(new ConversionGroup(group.Key, targetTenantId, versions));
        }

        if (tenantMap.Values.Where(value => value != Tenant.DefaultTenantId).Distinct(StringComparer.Ordinal).Count() != tenantMap.Count)
        {
            throw new BridgeRejectedException("TenantMappingCollision");
        }

        if (existing.Count > 0 && existing.Any(row => string.Equals(row.Id, SidecarTable, StringComparison.Ordinal)))
        {
            throw new BridgeRejectedException("TargetCollision");
        }

        return result;
    }

    private static string NormalizeLegacyTenant(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return Tenant.DefaultTenantId;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BridgeRejectedException("InvalidTenantId");
        }

        return value;
    }

    private static SecretStatus MapStatus(int status) => status switch
    {
        0 => SecretStatus.Active,
        1 => SecretStatus.Retired,
        2 => SecretStatus.Expired,
        3 => SecretStatus.Revoked,
        _ => throw new BridgeRejectedException("UnknownStatus")
    };

    private static async Task<IReadOnlyList<ExistingTarget>> ReadExistingTargetAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SecretsElsaDbContext>>();
        await using var dbContext = await factory.CreateDbContextAsync();
        var existing = await dbContext.Secrets.IgnoreQueryFilters().AsNoTracking().ToListAsync();
        return existing.Select(secret => new ExistingTarget(
            secret.Id,
            secret.TenantId,
            dbContext.Entry(secret).Property<string>(SecretShadowPropertyNames.NormalizedName).CurrentValue ?? string.Empty)).ToArray();
    }

    private static async Task PersistConversionAsync(
        IServiceProvider services,
        IReadOnlyList<Secret> secrets,
        IReadOnlyList<LegacyRow> source,
        string scenario)
    {
        await using var scope = services.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SecretsElsaDbContext>>();
        await using var dbContext = await factory.CreateDbContextAsync();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        await dbContext.Database.ExecuteSqlRawAsync(CreateSidecarSql);

        foreach (var secret in secrets)
        {
            dbContext.Secrets.Add(secret);
            var entry = dbContext.Entry(secret);
            entry.Property<string>(SecretShadowPropertyNames.NormalizedName).CurrentValue = new DefaultSecretNameValidator().Normalize(secret.Name);
            entry.Property<string>(SecretShadowPropertyNames.SerializedTags).CurrentValue = JsonSerializer.Serialize(secret.Tags.Order(StringComparer.OrdinalIgnoreCase), JsonOptions);
            entry.Property<string>(SecretShadowPropertyNames.SerializedVersions).CurrentValue = JsonSerializer.Serialize(secret.Versions, JsonOptions);
        }

        await dbContext.SaveChangesAsync();
        if (scenario == "fail-after-core-save")
        {
            throw new BridgeRejectedException("InjectedWriteFailure");
        }

        if (scenario != "success" && scenario != "id-collision")
        {
            throw new BridgeRejectedException("UnknownScenario");
        }

        var batchId = Guid.NewGuid().ToString("N");
        foreach (var row in source)
        {
            await using var command = dbContext.Database.GetDbConnection().CreateCommand();
            command.Transaction = transaction.GetDbTransaction();
            command.CommandText = InsertSidecarSql;
            AddParameter(command, "@legacyId", row.Id);
            AddParameter(command, "@legacySecretId", row.SecretId);
            AddParameter(command, "@legacyName", row.Name);
            AddParameter(command, "@legacyScope", row.ScopeRaw);
            AddParameter(command, "@legacyEncryptedValue", row.EncryptedValue);
            AddParameter(command, "@legacyDescription", row.Description);
            AddParameter(command, "@legacyVersion", row.Version);
            AddParameter(command, "@legacyIsLatest", row.IsLatest);
            AddParameter(command, "@legacyStatus", row.Status);
            AddParameter(command, "@legacyExpiresIn", row.ExpiresInRaw);
            AddParameter(command, "@legacyExpiresAt", row.ExpiresAtRaw);
            AddParameter(command, "@legacyLastAccessedAt", row.LastAccessedAtRaw);
            AddParameter(command, "@legacyTenantId", row.TenantIdRaw);
            AddParameter(command, "@legacyCreatedAt", row.CreatedAtRaw);
            AddParameter(command, "@legacyUpdatedAt", row.UpdatedAtRaw);
            AddParameter(command, "@legacyOwner", row.Owner);
            AddParameter(command, "@coreSecretId", row.SecretId);
            AddParameter(command, "@coreVersion", row.Version);
            AddParameter(command, "@migrationBatchId", batchId);
            await command.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private const string CreateSidecarSql = """
        CREATE TABLE [Elsa].[ElsaSecretsLegacyV381] (
            LegacyId nvarchar(450) NOT NULL PRIMARY KEY,
            LegacySecretId nvarchar(450) NOT NULL,
            LegacyName nvarchar(450) NOT NULL,
            LegacyScope nvarchar(max) NULL,
            LegacyEncryptedValue nvarchar(max) NOT NULL,
            LegacyDescription nvarchar(max) NOT NULL,
            LegacyVersion int NOT NULL,
            LegacyIsLatest bit NOT NULL,
            LegacyStatus int NOT NULL,
            LegacyExpiresIn nvarchar(64) NULL,
            LegacyExpiresAt nvarchar(64) NULL,
            LegacyLastAccessedAt nvarchar(64) NULL,
            LegacyTenantId nvarchar(450) NULL,
            LegacyCreatedAt nvarchar(64) NOT NULL,
            LegacyUpdatedAt nvarchar(64) NOT NULL,
            LegacyOwner nvarchar(max) NULL,
            CoreSecretId nvarchar(450) NOT NULL,
            CoreVersion int NOT NULL,
            MigrationBatchId nvarchar(36) NOT NULL,
            CONSTRAINT UQ_ElsaSecretsLegacyV381_CoreVersion UNIQUE (CoreSecretId, CoreVersion)
        );
        CREATE INDEX [IX_ElsaSecretsLegacyV381_SecretId] ON [Elsa].[ElsaSecretsLegacyV381] (LegacySecretId);
        CREATE INDEX [IX_ElsaSecretsLegacyV381_TenantId] ON [Elsa].[ElsaSecretsLegacyV381] (LegacyTenantId);
        """;

    private const string InsertSidecarSql = """
        INSERT INTO [Elsa].[ElsaSecretsLegacyV381] (
            LegacyId, LegacySecretId, LegacyName, LegacyScope, LegacyEncryptedValue, LegacyDescription,
            LegacyVersion, LegacyIsLatest, LegacyStatus, LegacyExpiresIn, LegacyExpiresAt, LegacyLastAccessedAt,
            LegacyTenantId, LegacyCreatedAt, LegacyUpdatedAt, LegacyOwner, CoreSecretId, CoreVersion, MigrationBatchId)
        VALUES (
            @legacyId, @legacySecretId, @legacyName, @legacyScope, @legacyEncryptedValue, @legacyDescription,
            @legacyVersion, @legacyIsLatest, @legacyStatus, @legacyExpiresIn, @legacyExpiresAt, @legacyLastAccessedAt,
            @legacyTenantId, @legacyCreatedAt, @legacyUpdatedAt, @legacyOwner, @coreSecretId, @coreVersion, @migrationBatchId);
        """;

    private static async Task<bool> VerifySidecarAsync(IServiceProvider services, IReadOnlyList<LegacyRow> source, IReadOnlyList<Secret> secrets)
    {
        await using var scope = services.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SecretsElsaDbContext>>();
        await using var dbContext = await factory.CreateDbContextAsync();
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT LegacyId, LegacySecretId, LegacyName, LegacyScope, LegacyEncryptedValue, LegacyDescription,
                   LegacyVersion, LegacyIsLatest, LegacyStatus, LegacyExpiresIn, LegacyExpiresAt,
                   LegacyLastAccessedAt, LegacyTenantId, LegacyCreatedAt, LegacyUpdatedAt, LegacyOwner,
                   CoreSecretId, CoreVersion
            FROM [Elsa].[ElsaSecretsLegacyV381] ORDER BY LegacyId;
            """;
        await using var reader = await command.ExecuteReaderAsync();
        var actual = new Dictionary<string, string[]>(StringComparer.Ordinal);
        while (await reader.ReadAsync())
        {
            actual.Add(reader.GetString(0), Enumerable.Range(1, 17).Select(index => reader.IsDBNull(index) ? "<NULL>" : Convert.ToString(reader.GetValue(index), CultureInfo.InvariantCulture)!).ToArray());
        }

        var expected = source.ToDictionary(
            row => row.Id,
            row => new[]
            {
                row.SecretId, row.Name, N(row.ScopeRaw), row.EncryptedValue, row.Description,
                row.Version.ToString(CultureInfo.InvariantCulture), row.IsLatest ? "True" : "False", row.Status.ToString(CultureInfo.InvariantCulture),
                N(row.ExpiresInRaw), N(row.ExpiresAtRaw), N(row.LastAccessedAtRaw), N(row.TenantIdRaw), row.CreatedAtRaw, row.UpdatedAtRaw,
                N(row.Owner), row.SecretId, row.Version.ToString(CultureInfo.InvariantCulture)
            }, StringComparer.Ordinal);
        return expected.Count == actual.Count && expected.All(pair => actual.TryGetValue(pair.Key, out var values) && pair.Value.SequenceEqual(values, StringComparer.Ordinal));
    }

    private static string N(string? value) => value ?? "<NULL>";

    private static async Task<int> ReadSidecarCountAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SecretsElsaDbContext>>();
        await using var dbContext = await factory.CreateDbContextAsync();
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM [Elsa].[ElsaSecretsLegacyV381]";
        return Convert.ToInt32(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }

    private static async Task<string[]> ReadTargetMigrationIdsAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SecretsElsaDbContext>>();
        await using var dbContext = await factory.CreateDbContextAsync();
        return (await dbContext.Database.GetAppliedMigrationsAsync()).ToArray();
    }

    private static async Task<bool> VerifyTenantRepositoryAsync(IServiceProvider services, DefaultTenantAccessor tenantAccessor)
    {
        var defaultRows = await ReadVisibleAsync(services, tenantAccessor, null);
        var tenantARows = await ReadVisibleAsync(services, tenantAccessor, "tenant-a");
        var tenantBRows = await ReadVisibleAsync(services, tenantAccessor, "tenant-b");

        if (defaultRows.SingleOrDefault(row => row.Name == "shared:service")?.Id != "legacy-aggregate-default")
        {
            return false;
        }

        if (tenantARows.SingleOrDefault(row => row.Name == "shared:service")?.Id != "legacy-aggregate-tenant-a")
        {
            return false;
        }

        if (tenantBRows.SingleOrDefault(row => row.Name == "shared:service")?.Id != "legacy-aggregate-tenant-b")
        {
            return false;
        }

        if (tenantARows.Any(row => row.Name == "tenant-b:exclusive") || tenantBRows.All(row => row.Name != "tenant-b:exclusive"))
        {
            return false;
        }

        using (tenantAccessor.PushContext(new Tenant { Id = "tenant-a", Name = "tenant-a" }))
        await using (var scope = services.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<EFCoreSecretRepository>();
            var tenantAStillIsolated = (await repository.ListAsync()).All(row => row.Name != "tenant-b:exclusive");
            var tenantBAfter = await ReadVisibleAsync(services, tenantAccessor, "tenant-b");
            var tenantBRowUnchanged = tenantBAfter.SingleOrDefault(row => row.Name == "tenant-b:exclusive")?.Id == "legacy-aggregate-tenant-b-exclusive";
            return tenantAStillIsolated && tenantBRowUnchanged;
        }
    }

    private static async Task<IReadOnlyList<Secret>> ReadVisibleAsync(IServiceProvider services, DefaultTenantAccessor tenantAccessor, string? tenantId)
    {
        using var tenantContext = tenantId == null ? null : tenantAccessor.PushContext(new Tenant { Id = tenantId, Name = tenantId });
        await using var scope = services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<EFCoreSecretRepository>();
        return (await repository.ListAsync()).ToArray();
    }

    private static async Task<IReadOnlyList<Secret>> ReadAllTenantVisibleAsync(IServiceProvider services, DefaultTenantAccessor tenantAccessor)
    {
        var result = new List<Secret>();
        result.AddRange(await ReadVisibleAsync(services, tenantAccessor, null));
        result.AddRange(await ReadVisibleAsync(services, tenantAccessor, "tenant-a"));
        result.AddRange(await ReadVisibleAsync(services, tenantAccessor, "tenant-b"));
        return result;
    }

    private static async Task<EncryptionResult> VerifyEncryptionAsync(
        IReadOnlyList<Secret> secrets,
        IReadOnlyList<LegacyRow> source,
        IReadOnlyDictionary<string, string> plaintextByLegacyId,
        EncryptedSecretStore encryptedStore,
        DefaultSecretValueProtector coreProtector)
    {
        var sourceHashById = source.ToDictionary(row => row.Id, row => Hash(plaintextByLegacyId[row.Id]), StringComparer.Ordinal);
        var rawLegacyCiphertextRejected = true;
        var wrongCoreStore = new EncryptedSecretStore(new DefaultSecretValueProtector(Options.Create(new SecretsOptions { EncryptionKey = WrongCoreKey })));
        var wrongCoreKeyRejected = true;
        var rowByAggregateVersion = source.ToDictionary(row => (row.SecretId, row.Version));

        foreach (var secret in secrets)
        {
            foreach (var version in secret.Versions)
            {
                var oldRow = rowByAggregateVersion[(secret.Id, version.Version)];
                var payload = await encryptedStore.ReadAsync(secret, version);
                if (payload?.Value == null || Hash(payload.Value) != sourceHashById[oldRow.Id])
                {
                    return new EncryptionResult(false, false, false, false);
                }

                var legacyCiphertextCopy = new SecretVersion
                {
                    Version = version.Version,
                    Payload = new SecretPayload { Metadata = { ["protectedValue"] = oldRow.EncryptedValue } }
                };
                try
                {
                    _ = await encryptedStore.ReadAsync(secret, legacyCiphertextCopy);
                    rawLegacyCiphertextRejected = false;
                }
                catch (Exception error) when (error is CryptographicException or InvalidOperationException)
                {
                    _ = error;
                    // Failure is the expected proof that legacy ciphertext is not a Core payload.
                }

                try
                {
                    _ = await wrongCoreStore.ReadAsync(secret, version);
                    wrongCoreKeyRejected = false;
                }
                catch (CryptographicException error)
                {
                    _ = error;
                    // Failure is the expected proof that the independent wrong Core key is rejected.
                }
            }
        }

        var missingCoreKeyRejected = false;
        try
        {
            _ = new DefaultSecretValueProtector(Options.Create(new SecretsOptions())).Protect("synthetic");
        }
        catch (InvalidOperationException)
        {
            missingCoreKeyRejected = true;
        }

        return new EncryptionResult(
            rawLegacyCiphertextRejected && wrongCoreKeyRejected && missingCoreKeyRejected,
            wrongCoreKeyRejected,
            missingCoreKeyRejected,
            rawLegacyCiphertextRejected);
    }

    private static bool CanUnprotect(IDataProtector protector, string ciphertext)
    {
        try
        {
            _ = protector.Unprotect(ciphertext);
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static IDataProtectionProvider CreateDataProtectionProvider(string keyRingPath, string applicationName = DataProtectionApplicationName)
    {
        Directory.CreateDirectory(keyRingPath);
        var services = new ServiceCollection();
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keyRingPath))
            .SetApplicationName(applicationName);
        return services.BuildServiceProvider().GetRequiredService<IDataProtectionProvider>();
    }

    private static async Task SeedAggregateIdCollisionAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SecretsElsaDbContext>>();
        await using var dbContext = await factory.CreateDbContextAsync();
        var secret = new Secret
        {
            Id = "legacy-aggregate-default",
            TenantId = Tenant.DefaultTenantId,
            Name = "preexisting:target",
            DisplayName = "preexisting:target",
            TypeName = SecretTypeNames.Text,
            StoreName = SecretStoreNames.Encrypted,
            Status = SecretStatus.Active
        };
        dbContext.Secrets.Add(secret);
        SetSerializedProperties(dbContext, secret);
        await dbContext.SaveChangesAsync();
    }

    private static void SetSerializedProperties(SecretsElsaDbContext dbContext, Secret secret)
    {
        var entry = dbContext.Entry(secret);
        entry.Property<string>(SecretShadowPropertyNames.NormalizedName).CurrentValue = new DefaultSecretNameValidator().Normalize(secret.Name);
        entry.Property<string>(SecretShadowPropertyNames.SerializedTags).CurrentValue = JsonSerializer.Serialize(secret.Tags.Order(StringComparer.OrdinalIgnoreCase), JsonOptions);
        entry.Property<string>(SecretShadowPropertyNames.SerializedVersions).CurrentValue = JsonSerializer.Serialize(secret.Versions, JsonOptions);
    }

    private static async Task<TargetSnapshot> SnapshotTargetAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SecretsElsaDbContext>>();
        await using var dbContext = await factory.CreateDbContextAsync();
        var secrets = await dbContext.Secrets.IgnoreQueryFilters().AsNoTracking().OrderBy(secret => secret.Id).ToListAsync();
        var rows = secrets.Select(secret =>
        {
            var entry = dbContext.Entry(secret);
            return new
            {
                secret.Id,
                secret.TenantId,
                secret.Name,
                secret.DisplayName,
                secret.Description,
                secret.TypeName,
                secret.StoreName,
                secret.Scope,
                secret.ManagedOwnerId,
                secret.ManagedGenerationId,
                secret.Status,
                secret.CreatedAt,
                secret.UpdatedAt,
                NormalizedName = entry.Property<string>(SecretShadowPropertyNames.NormalizedName).CurrentValue,
                SerializedTags = entry.Property<string>(SecretShadowPropertyNames.SerializedTags).CurrentValue,
                SerializedVersions = entry.Property<string>(SecretShadowPropertyNames.SerializedVersions).CurrentValue
            };
        }).ToArray();
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var sidecarCommand = connection.CreateCommand();
        sidecarCommand.CommandText = SidecarExistsSql;
        var sidecarExists = Convert.ToInt32(await sidecarCommand.ExecuteScalarAsync(), CultureInfo.InvariantCulture) > 0;
        var sidecarCount = sidecarExists ? await ReadSidecarCountAsync(dbContext) : -1;
        var migrationIds = await dbContext.Database.GetAppliedMigrationsAsync();
        var snapshot = JsonSerializer.Serialize(new
        {
            rows,
            sidecarExists,
            sidecarCount,
            migrationIds
        }, JsonOptions);
        return new TargetSnapshot(secrets.Select(secret => secret.Id).ToArray(), Hash(snapshot));
    }

    private static async Task<int> ReadSidecarCountAsync(SecretsElsaDbContext dbContext)
    {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM [Elsa].[ElsaSecretsLegacyV381]";
        return Convert.ToInt32(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }

    private const string SidecarExistsSql = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='Elsa' AND table_name='ElsaSecretsLegacyV381'";

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private sealed record LegacyRow(
        string Id,
        string SecretId,
        string Name,
        string? ScopeRaw,
        string EncryptedValue,
        string Description,
        int Version,
        bool IsLatest,
        int Status,
        string? ExpiresInRaw,
        string? ExpiresAtRaw,
        string? LastAccessedAtRaw,
        string? TenantIdRaw,
        string CreatedAtRaw,
        string UpdatedAtRaw,
        string? Owner)
    {
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public DateTimeOffset? LastAccessedAt { get; set; }
    }

    private sealed record ConversionGroup(string Key, string TargetTenantId, LegacyRow[] Rows);
    private sealed record ExistingTarget(string Id, string? TenantId, string NormalizedName);
    private sealed record TargetSnapshot(IReadOnlyList<string> SecretIds, string Digest);
    private sealed record EncryptionResult(bool RoundTrip, bool WrongCoreKeyRejected, bool MissingCoreKeyRejected, bool RawLegacyCiphertextRejected);

    private sealed class BridgeRejectedException(
        string code,
        bool conversionWritesUnchanged = true,
        bool originalDestinationUnchanged = true) : Exception
    {
        public string Code { get; } = code;
        public bool ConversionWritesUnchanged { get; } = conversionWritesUnchanged;
        public bool OriginalDestinationUnchanged { get; } = originalDestinationUnchanged;
    }
}
