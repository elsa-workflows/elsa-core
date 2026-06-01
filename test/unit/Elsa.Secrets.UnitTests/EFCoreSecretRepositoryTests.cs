using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Secrets.Models;
using Elsa.Secrets.Persistence.EFCore;
using Elsa.Secrets.Persistence.EFCore.Repositories;
using Elsa.Secrets.Persistence.EFCore.Sqlite.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.Secrets.UnitTests;

public class EFCoreSecretRepositoryTests : IAsyncLifetime
{
    private readonly string _databasePath = Path.Join(Path.GetTempPath(), $"elsa-secrets-{Guid.NewGuid():N}.db");
    private readonly ServiceProvider _serviceProvider;

    public EFCoreSecretRepositoryTests()
    {
        var services = new ServiceCollection();
        var connectionString = $"Data Source={_databasePath}";
        services.AddSqliteEntityModelCreatingHandlers();
        services.AddDbContextFactory<SecretsElsaDbContext>(builder => builder.UseElsaSqlite(typeof(SqliteSecretsPersistenceFeatureExtensions).Assembly, connectionString));
        services.AddScoped<Store<SecretsElsaDbContext, Secret>>();
        services.AddScoped<EFCoreSecretRepository>();
        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task InitializeAsync()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SecretsElsaDbContext>>();
        await using var dbContext = await factory.CreateDbContextAsync();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();

        if (File.Exists(_databasePath))
            File.Delete(_databasePath);
    }

    [Fact]
    public async Task PersistsSecretAggregate()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<EFCoreSecretRepository>();
        var secret = new Secret
        {
            Name = "smtp:password",
            DisplayName = "SMTP password",
            Tags = ["API-Key"],
            Versions = { new SecretVersion { Version = 1, Payload = new SecretPayload { Metadata = { ["ProtectedValue"] = "ciphertext" } } } }
        };

        await repository.AddAsync(secret);
        var reloaded = await repository.GetAsync("smtp:password");

        Assert.NotNull(reloaded);
        Assert.Equal("SMTP password", reloaded.DisplayName);
        Assert.Contains("api-key", reloaded.Tags);
        Assert.True(reloaded.Versions.Single().Payload.Metadata.ContainsKey("protectedvalue"));
        Assert.Equal(1, reloaded.Versions.Single().Version);
    }

    [Fact]
    public async Task TryAddOrReplaceDeletedAsync_ReplacesOnlyDeletedSecret()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<EFCoreSecretRepository>();
        await repository.AddAsync(new Secret { Name = "smtp:password", DisplayName = "SMTP password" });

        var activeReplacementResult = await repository.TryAddOrReplaceDeletedAsync(new Secret { Name = "smtp:password", DisplayName = "Active replacement" });
        await repository.SaveAsync(new Secret { Name = "smtp:password", DisplayName = "Deleted password", Status = SecretStatus.Deleted });
        var deletedReplacementResult = await repository.TryAddOrReplaceDeletedAsync(new Secret { Name = "smtp:password", DisplayName = "Replacement password" });
        var reloaded = await repository.GetAsync("smtp:password");

        Assert.False(activeReplacementResult);
        Assert.True(deletedReplacementResult);
        Assert.NotNull(reloaded);
        Assert.Equal("Replacement password", reloaded.DisplayName);
        Assert.Equal(SecretStatus.Active, reloaded.Status);
    }

    [Fact]
    public async Task NameLookups_AreCaseInsensitive()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<EFCoreSecretRepository>();
        await repository.AddAsync(new Secret { Name = "SMTP:PASSWORD", DisplayName = "SMTP password" });

        var reloaded = await repository.GetAsync("smtp:password");
        var whitespaceReloaded = await repository.GetAsync(" SMTP:PASSWORD ");
        var activeReplacementResult = await repository.TryAddOrReplaceDeletedAsync(new Secret { Name = "smtp:password", DisplayName = "Replacement password" });
        var duplicateException = await Assert.ThrowsAsync<InvalidOperationException>(() => repository.AddAsync(new Secret { Name = "smtp:password", DisplayName = "Duplicate password" }));

        Assert.NotNull(reloaded);
        Assert.NotNull(whitespaceReloaded);
        Assert.Equal("SMTP:PASSWORD", reloaded.Name);
        Assert.Equal(reloaded.Id, whitespaceReloaded.Id);
        Assert.False(activeReplacementResult);
        Assert.Equal("A secret named 'smtp:password' already exists.", duplicateException.Message);
    }

    [Fact]
    public async Task TryAddOrReplaceDeletedAsync_WhenReplacingDeletedSecret_PersistsReplacementId()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<EFCoreSecretRepository>();
        await repository.SaveAsync(new Secret { Id = "old", Name = "smtp:password", DisplayName = "Deleted password", Status = SecretStatus.Deleted });

        var replacement = new Secret { Id = "new", Name = "SMTP:PASSWORD", DisplayName = "Replacement password" };
        var result = await repository.TryAddOrReplaceDeletedAsync(replacement);
        var reloaded = await repository.GetAsync("smtp:password");

        Assert.True(result);
        Assert.NotNull(reloaded);
        Assert.Equal("new", reloaded.Id);
        Assert.Equal("Replacement password", reloaded.DisplayName);
        Assert.Equal(SecretStatus.Active, reloaded.Status);
    }
}
