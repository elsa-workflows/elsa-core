using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Elsa.Persistence.YesSql.Data;
using Elsa.Persistence.YesSql.Documents;
using Microsoft.Extensions.Logging;
using YesSql;
using YesSql.Sql;

namespace Elsa.Persistence.YesSql.Services
{
    /// <summary>
    /// Represents a class that manages the database migrations.
    /// </summary>
    public class DataMigrationManager : IDataMigrationManager
    {
        private readonly IEnumerable<IDataMigration> _dataMigrations;
        private readonly ISession _session;
        private readonly IStore _store;
        private readonly ILogger _logger;
        private readonly IList<IDataMigration> _processedMigrations;

        private DataMigrationsDocument? _dataMigrationsDocument;

        public DataMigrationManager(
            IEnumerable<IDataMigration> dataMigrations,
            ISession session,
            IStore store,
            ILogger<DataMigrationManager> logger)
        {
            _dataMigrations = dataMigrations;
            _session = session;
            _store = store;
            _logger = logger;
            _processedMigrations = new List<IDataMigration>();
        }

        public async Task RunAllAsync()
        {
            var migrationsToUpdate = await GetMigrationsThatNeedUpdateAsync();

            foreach (var migration in migrationsToUpdate)
            {
                try
                {
                    await UpdateAsync(migration);
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    _logger.LogError(ex, "Could not run migrations automatically on '{FeatureName}'", migration);
                }
            }

            await _session.SaveChangesAsync();
        }

        public async Task<IEnumerable<IDataMigration>> GetMigrationsThatNeedUpdateAsync()
        {
            var currentVersions = (await GetOrCreateDataMigrationsDocumentAsync()).DataMigrations.ToDictionary(r => r.DataMigrationClass);

            var outOfDateMigrations = _dataMigrations.Where(
                dataMigration =>
                {
                    var dataMigrationTypeName = dataMigration.GetType().FullName!;

                    return currentVersions.TryGetValue(dataMigrationTypeName, out var record) && record.Version.HasValue
                        ? CreateUpgradeLookupTable(dataMigration).ContainsKey(record.Version.Value)
                        : (GetCreateMethod(dataMigration) ?? GetCreateAsyncMethod(dataMigration)) != null;
                });

            return outOfDateMigrations;
        }

        private async Task UpdateAsync(IDataMigration dataMigration)
        {
            if (_processedMigrations.Contains(dataMigration))
                return;

            var migrationName = dataMigration.GetType().FullName;

            _processedMigrations.Add(dataMigration);
            _logger.LogInformation("Running migration '{MigrationName}'", migrationName);

            // Apply update methods to migration class.

            var schemaBuilder = new SchemaBuilder(_store.Configuration, await _session.BeginTransactionAsync());
            dataMigration.SchemaBuilder = schemaBuilder;

            // Copy the object for the Linq query.
            var tempMigration = dataMigration;

            // Get current version for this migration.
            var dataMigrationRecord = await GetDataMigrationRecordAsync(tempMigration);

            var current = 0;
            var dataMigrationsDocument = await GetOrCreateDataMigrationsDocumentAsync();

            if (dataMigrationRecord != null)
            {
                // Version can be null if a failed create migration has occurred and the data migration record was saved.
                current = dataMigrationRecord.Version ?? current;
            }
            else
            {
                dataMigrationRecord = new DataMigrationRecord { DataMigrationClass = dataMigration.GetType().FullName! };

                dataMigrationsDocument.DataMigrations.Add(dataMigrationRecord);
            }

            try
            {
                // Do we need to call Create()?
                if (current == 0)
                {
                    // try to resolve a Create method.
                    var createMethod = GetCreateMethod(dataMigration);

                    if (createMethod != null)
                        current = (int) createMethod.Invoke(dataMigration, new object[0])!;

                    // try to resolve a CreateAsync method.
                    var createAsyncMethod = GetCreateAsyncMethod(dataMigration);

                    if (createAsyncMethod != null)
                        current = await (Task<int>) createAsyncMethod.Invoke(dataMigration, new object[0])!;
                }

                var lookupTable = CreateUpgradeLookupTable(dataMigration);

                while (lookupTable.TryGetValue(current, out var methodInfo))
                {
                    _logger.LogInformation(
                        "Applying migration '{MigrationName}' from version {Version}",
                        migrationName,
                        current);

                    var isAwaitable = methodInfo.ReturnType.GetMethod(nameof(Task.GetAwaiter)) != null;

                    current = isAwaitable
                        ? await (Task<int>) methodInfo.Invoke(dataMigration, new object[0])!
                        : (int) methodInfo.Invoke(dataMigration, new object[0])!;
                }

                // If current is 0, it means no upgrade/create method was found or succeeded.
                if (current == 0)
                    return;

                dataMigrationRecord.Version = current;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while running migration version {Version} for '{MigrationName}'",
                    current,
                    migrationName);
            }
            finally
            {
                // Persist data migrations
                _session.Save(dataMigrationsDocument);
            }
        }

        private async Task<DataMigrationsDocument> GetOrCreateDataMigrationsDocumentAsync()
        {
            if (_dataMigrationsDocument != null)
                return _dataMigrationsDocument;

            _dataMigrationsDocument = await _session.Query<DataMigrationsDocument>().FirstOrDefaultAsync();

            if (_dataMigrationsDocument != null)
                return _dataMigrationsDocument;

            _dataMigrationsDocument = new DataMigrationsDocument();
            _session.Save(_dataMigrationsDocument);

            return _dataMigrationsDocument;
        }

        private async Task<DataMigrationRecord?> GetDataMigrationRecordAsync(IDataMigration migration)
        {
            var dataMigrationsDocument = await GetOrCreateDataMigrationsDocumentAsync();
            var records = dataMigrationsDocument.DataMigrations;
            return records.FirstOrDefault(x => x.DataMigrationClass == migration.GetType().FullName);
        }

        /// <summary>
        /// Create a list of all available Update methods from a data migration class, indexed by the version number.
        /// </summary>
        private static Dictionary<int, MethodInfo> CreateUpgradeLookupTable(IDataMigration dataMigration)
        {
            return dataMigration
                .GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Select(GetUpdateMethod)
                .Where(tuple => tuple != null)
                .Select(tuple => tuple!)
                .ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);
        }

        private static Tuple<int, MethodInfo>? GetUpdateMethod(MethodInfo methodInfo)
        {
            const string updateFromPrefix = "UpdateFrom";
            const string asyncSuffix = "Async";

            if (!methodInfo.Name.StartsWith(updateFromPrefix, StringComparison.Ordinal) ||
                (methodInfo.ReturnType != typeof(int) && methodInfo.ReturnType != typeof(Task<int>))) return null;

            var version = methodInfo.Name.EndsWith(asyncSuffix, StringComparison.Ordinal)
                ? methodInfo.Name.Substring(
                    updateFromPrefix.Length,
                    methodInfo.Name.Length - updateFromPrefix.Length - asyncSuffix.Length)
                : methodInfo.Name.Substring(updateFromPrefix.Length);

            return int.TryParse(version, out var versionValue)
                ? new Tuple<int, MethodInfo>(versionValue, methodInfo)
                : null;
        }

        /// <summary>
        /// Returns the Create method from a data migration class if it's found.
        /// </summary>
        private static MethodInfo? GetCreateMethod(IDataMigration dataMigration) => GetMethod(dataMigration, "Create");

        /// <summary>
        /// Returns the CreateAsync method from a data migration class if it's found
        /// </summary>
        private static MethodInfo? GetCreateAsyncMethod(IDataMigration dataMigration) => GetMethod(dataMigration, "CreateAsync");

        /// <summary>
        /// Returns the CreateAsync method from a data migration class if it's found
        /// </summary>
        private static MethodInfo? GetMethod(IDataMigration dataMigration, string name)
        {
            var flags = BindingFlags.Public | BindingFlags.Instance;
            var methodInfo = dataMigration.GetType().GetMethod(name, flags);
            var returnType = methodInfo?.ReturnType;
            return returnType != null && (returnType == typeof(Task<int>) || returnType == typeof(int)) ? methodInfo : null;
        }
    }
}