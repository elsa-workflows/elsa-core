using System.Text.Json;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Policies;
using Elsa.ExternalAuthentication.Services;
using System.Threading.Tasks;

namespace Elsa.ExternalAuthentication.UnitTests.Extensibility;

public class ExtensionConformanceTests
{
    [Test]
    public async Task AdapterRegistryIsDeterministicDeploymentBoundedAndRejectsDuplicateIds()
    {
        var validator = new ExtensionDescriptorValidator();
        var options = OptionsFor(allowedAdapters: ["second"]);
        var first = new ConformanceAdapter("first");
        var second = new ConformanceAdapter("second");

        var registry = new DefaultExternalAuthenticationAdapterRegistry([second, first], validator, Microsoft.Extensions.Options.Options.Create(options));

        await Assert.That(registry.ListDescriptors().Select(x => x.Type)).IsEquivalentTo(["second"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(registry.TryGet("first", out _)).IsFalse();
        await Assert.That(registry.TryGet("second", out var selected)).IsTrue();
        await Assert.That(selected).IsSameReferenceAs(second);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            new DefaultExternalAuthenticationAdapterRegistry(
                [first, new ConformanceAdapter("first")],
                validator,
                Microsoft.Extensions.Options.Options.Create(OptionsFor())));
    }

    [Test]
    public async Task PolicyAndGrantSourceRegistriesExposeOnlyDeploymentAllowedExtensions()
    {
        var validator = new ExtensionDescriptorValidator();
        var options = OptionsFor();
        options.AllowedUnlinkedIdentityPolicyTypes = ["custom-policy"];
        options.AllowedPermissionGrantSourceTypes = ["custom-grants"];

        var policies = new DefaultUnlinkedIdentityPolicyRegistry(
            [new ConformancePolicy("hidden-policy"), new ConformancePolicy("custom-policy")],
            [],
            validator,
            Microsoft.Extensions.Options.Options.Create(options));
        var sources = new DefaultPermissionGrantSourceRegistry(
            [new ConformanceGrantSource("hidden-grants"), new ConformanceGrantSource("custom-grants")],
            validator,
            Microsoft.Extensions.Options.Options.Create(options));

        await Assert.That(policies.ListDescriptors().Select(x => x.Type)).IsEquivalentTo(["custom-policy"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(sources.ListDescriptors().Select(x => x.Type)).IsEquivalentTo(["custom-grants"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(policies.TryGet("hidden-policy", out _)).IsFalse();
        await Assert.That(sources.TryGet("hidden-grants", out _)).IsFalse();
    }

    [Test]
    public async Task DescriptorValidatorRejectsMismatchedUnsafeAndIncompleteMetadata()
    {
        var invalidField = Field("secret", valueType: "string", secret: true, redacted: false) with
        {
            VisibleWhen = new SettingFieldVisibilityCondition("secret", "yes")
        };
        var adapter = new ConformanceAdapter(
            "valid-type",
            new ExternalAuthenticationAdapterDescriptor(
                "different-type",
                "",
                "",
                0,
                [invalidField],
                new ExternalAuthenticationAdapterCapabilities(true, true, true),
                new CustomEditorContract("", 0)));

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => new ExtensionDescriptorValidator().Validate(adapter));

        await Assert.That(exception.Message).Contains("does not match");
        await Assert.That(exception.Message).Contains("positive settings version");
        await Assert.That(exception.Message).Contains("Secret-binding field");
        await Assert.That(exception.Message).Contains("visibility condition");
        await Assert.That(exception.Message).Contains("custom-editor");
    }

    [Test]
    public async Task DescriptorValidatorAcceptsLowerCamelCaseSettingFieldNames()
    {
        var descriptor = new ExtensionDescriptorValidator().Validate(new CreateUserUnlinkedIdentityPolicy());

        await Assert.That(descriptor.Fields).Contains(field => field.Name == "defaultRoleIds");
    }

    [Test]
    public async Task DescriptorValidatorKeepsExtensionTypesLowercaseAndStable()
    {
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
            new ExtensionDescriptorValidator().Validate(new ConformanceAdapter("InvalidType")));

        await Assert.That(exception.Message).Contains("stable identifier");
    }

    [Test]
    public async Task SettingsMigrationRunsAdapterOwnedForwardStepsAndRejectsUnsupportedVersions()
    {
        var adapter = new ConformanceAdapter("conformance", settingsVersion: 3);
        var registry = new DefaultExternalAuthenticationAdapterRegistry(
            [adapter],
            new ExtensionDescriptorValidator(),
            Microsoft.Extensions.Options.Options.Create(OptionsFor()));
        var service = new AdapterSettingsMigrationService(
            registry,
            [
                new MigrationStep("conformance", 1, 2, settings => new { value = settings.GetProperty("value").GetString(), migratedTo = 2 }),
                new MigrationStep("conformance", 2, 3, settings => new { value = settings.GetProperty("value").GetString(), migratedTo = 3 })
            ]);

        var migrated = await service.MigrateAsync(
            "conformance",
            1,
            JsonSerializer.SerializeToElement(new { value = "opaque" }));
        var current = await service.MigrateAsync("conformance", 3, migrated.Settings);

        await Assert.That(migrated.WasMigrated).IsTrue();
        await Assert.That(migrated.SettingsVersion).IsEqualTo(3);
        await Assert.That(migrated.Settings.GetProperty("value").GetString()).IsEqualTo("opaque");
        await Assert.That(migrated.Settings.GetProperty("migratedTo").GetInt32()).IsEqualTo(3);
        await Assert.That(current.WasMigrated).IsFalse();
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.MigrateAsync("unsupported", 1, JsonSerializer.SerializeToElement(new { })).AsTask());
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.MigrateAsync("conformance", 4, JsonSerializer.SerializeToElement(new { })).AsTask());
    }

    private static ExternalAuthenticationOptions OptionsFor(ICollection<string>? allowedAdapters = null) => new()
    {
        AllowedAdapterTypes = allowedAdapters ?? [],
        AllowedUnlinkedIdentityPolicyTypes = [],
        AllowedPermissionGrantSourceTypes = []
    };

    private static SettingFieldDescriptor Field(
        string name,
        string valueType = "string",
        bool secret = false,
        bool redacted = false) => new(
        name,
        "Field",
        "A conformance field.",
        valueType,
        false,
        "text",
        null,
        [],
        new SettingFieldValidation(),
        secret,
        false,
        null,
        null,
        redacted);

    private sealed class ConformanceAdapter : IExternalAuthenticationAdapter
    {
        private readonly ExternalAuthenticationAdapterDescriptor _descriptor;

        public ConformanceAdapter(string type, int settingsVersion = 1) : this(
            type,
            new ExternalAuthenticationAdapterDescriptor(
                type,
                $"Adapter {type}",
                "A conformance adapter.",
                settingsVersion,
                [Field("endpoint", "uri")],
                new ExternalAuthenticationAdapterCapabilities(true, true, true),
                new CustomEditorContract("conformance-editor", 1)))
        {
        }

        public ConformanceAdapter(string type, ExternalAuthenticationAdapterDescriptor descriptor)
        {
            Type = type;
            _descriptor = descriptor;
        }

        public string Type { get; }
        public ExternalAuthenticationAdapterDescriptor Describe() => _descriptor;
        public ValueTask<ConnectionValidationResult> ValidateAsync(ConnectionValidationContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ExternalAuthorizationRequest> CreateAuthorizationRequestAsync(ExternalAuthorizationContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ExternalAuthenticationResult> AuthenticateCallbackAsync(ExternalCallbackContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ConnectionTestResult> TestAsync(ConnectionTestContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ExternalLogoutRequest?> CreateLogoutRequestAsync(ExternalLogoutContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class ConformancePolicy(string type) : IUnlinkedIdentityPolicy
    {
        public string Type => type;
        public UnlinkedIdentityPolicyDescriptor Describe() => new(type, $"Policy {type}", "A conformance policy.", 1, [], null);
        public ValueTask<UnlinkedIdentityDecision> EvaluateAsync(UnlinkedIdentityContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class ConformanceGrantSource(string type) : IPermissionGrantSource
    {
        public string Type => type;
        public PermissionGrantSourceDescriptor Describe() => new(type, $"Source {type}", "A conformance grant source.", 1, [], null);
        public ValueTask<PermissionGrantResult> GetGrantsAsync(PermissionGrantContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class MigrationStep(
        string adapterType,
        int fromVersion,
        int toVersion,
        Func<JsonElement, object> migrate) : IAdapterSettingsMigration
    {
        public string AdapterType => adapterType;
        public int FromVersion => fromVersion;
        public int ToVersion => toVersion;
        public ValueTask<JsonElement> MigrateAsync(JsonElement settings, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(JsonSerializer.SerializeToElement(migrate(settings)));
        }
    }
}
