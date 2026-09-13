using System.Text.Json;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Expressions.Options;
using Elsa.Expressions.Services;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Expressions;
using Elsa.Secrets.Extensions;
using Elsa.Secrets.Models;
using Elsa.Secrets.Providers;
using Elsa.Workflows.Models;
using Elsa.Workflows.Serialization.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.UnitTests;

public class SecretExpressionTests
{
    private readonly SecretTestFixture _fixture = new();
    private readonly IWellKnownTypeRegistry _wellKnownTypeRegistry = new WellKnownTypeRegistry(Microsoft.Extensions.Options.Options.Create(new ExpressionOptions()));
    private readonly SecretExpressionHandler _handler;

    public SecretExpressionTests()
    {
        _handler = new(_fixture.Resolver, _wellKnownTypeRegistry);
    }

    [Test]
    public async Task EvaluateAsync_ResolvesSecretReference()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "api:key", Value = "top-secret" });

        var result = await EvaluateAsync<string>(new("api:key"));

        await Assert.That(result).IsEqualTo("top-secret");
    }

    [Test]
    public async Task EvaluateAsync_Throws_WhenSecretIsMissing()
    {
        var exception = await Assert.That(
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => EvaluateAsync<string>(new("api:key")))).IsNotNull();

        await Assert.That(exception.Message).IsEqualTo("Secret 'api:key' was not found.");
    }

    [Test]
    public async Task EvaluateAsync_Throws_WhenSecretTypeDoesNotMatchReference()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "api:key", TypeName = SecretTypeNames.Text, Value = "top-secret" });

        var exception = await Assert.That(
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => EvaluateAsync<string>(new("api:key", SecretTypeNames.RsaKey)))).IsNotNull();

        await Assert.That(exception.Message).IsEqualTo("Secret 'api:key' is not compatible with required type 'rsa-key'.");
    }

    [Test]
    public async Task EvaluateAsync_Throws_WhenSecretScopeDoesNotMatchReference()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "api:key", Scope = "production", Value = "top-secret" });

        var exception = await Assert.That(
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => EvaluateAsync<string>(new("api:key", Scope: "development")))).IsNotNull();

        await Assert.That(exception.Message).IsEqualTo("Secret 'api:key' is not compatible with required scope 'development'.");
    }

    [Test]
    public async Task EvaluateAsync_PassesCancellationTokenToResolver()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var resolver = new CapturingSecretResolver("top-secret");
        var handler = new SecretExpressionHandler(resolver, _wellKnownTypeRegistry);
        var context = CreateContext(cancellationTokenSource.Token);

        await handler.EvaluateAsync(SecretExpression.Create(new("api:key")), typeof(string), context, ExpressionEvaluatorOptions.Empty);

        await Assert.That(resolver.CancellationToken).IsEqualTo(cancellationTokenSource.Token);
    }

    [Test]
    public async Task SecretExpression_RoundTripsAsSecretReference()
    {
        var options = CreateSerializerOptions();
        var expression = SecretExpression.Create(new("api:key", SecretTypeNames.Text, "production"));

        var json = JsonSerializer.Serialize(expression, options);
        var deserializedExpression = await Assert.That(JsonSerializer.Deserialize<Expression>(json, options)).IsNotNull();
        var deserializedValue = await Assert.That(deserializedExpression.Value).IsNotNull();
        await Assert.That(deserializedValue).IsOfType(typeof(SecretReference));
        var deserializedReference = (SecretReference)deserializedValue;

        await Assert.That(json).Contains("\"type\":\"Secret\"").WithComparison(StringComparison.CurrentCulture);
        await Assert.That(json).Contains("\"name\":\"api:key\"").WithComparison(StringComparison.CurrentCulture);
        await Assert.That(json).Contains("\"typeName\":\"text\"").WithComparison(StringComparison.CurrentCulture);
        await Assert.That(json).Contains("\"scope\":\"production\"").WithComparison(StringComparison.CurrentCulture);
        await Assert.That(json).DoesNotContain("top-secret").WithComparison(StringComparison.CurrentCulture);
        await Assert.That(deserializedExpression.Type).IsEqualTo(SecretExpression.TypeName);
        await Assert.That(deserializedReference).IsEqualTo(new SecretReference("api:key", SecretTypeNames.Text, "production"));
    }

    [Test]
    public async Task SecretExpression_DeserializesEmptyStringAsNullReference()
    {
        var options = CreateSerializerOptions();
        const string json = """{"type":"Secret","value":""}""";

        var expression = await Assert.That(JsonSerializer.Deserialize<Expression>(json, options)).IsNotNull();

        await Assert.That(expression.Type).IsEqualTo(SecretExpression.TypeName);
        await Assert.That(expression.Value).IsNull();
    }

    [Test]
    public async Task SecretExpression_DeserializesStringAsSecretName()
    {
        var options = CreateSerializerOptions();
        const string json = """{"type":"Secret","value":"api:key"}""";

        var expression = await Assert.That(JsonSerializer.Deserialize<Expression>(json, options)).IsNotNull();

        var value = await Assert.That(expression.Value).IsNotNull();
        await Assert.That(value).IsOfType(typeof(SecretReference));
        var reference = (SecretReference)value;
        await Assert.That(reference).IsEqualTo(new SecretReference("api:key"));
    }

    [Test]
    public async Task SecretExpression_DeserializesStringifiedSecretReference()
    {
        var options = CreateSerializerOptions();
        const string json = """{"type":"Secret","value":"{\"name\":\"api:key\",\"typeName\":\"text\",\"scope\":\"production\"}"}""";

        var expression = await Assert.That(JsonSerializer.Deserialize<Expression>(json, options)).IsNotNull();

        var value = await Assert.That(expression.Value).IsNotNull();
        await Assert.That(value).IsOfType(typeof(SecretReference));
        var reference = (SecretReference)value;
        await Assert.That(reference).IsEqualTo(new SecretReference("api:key", SecretTypeNames.Text, "production"));
    }

    [Test]
    public async Task WorkflowInputJson_StoresSecretReferenceNotSecretValue()
    {
        var options = CreateSerializerOptions();
        var input = new Input<string>(SecretExpression.Create(new("api:key", SecretTypeNames.Text, "production")));

        var json = JsonSerializer.Serialize(input, options);
        var deserializedInput = await Assert.That(JsonSerializer.Deserialize<Input<string>>(json, options)).IsNotNull();
        var expression = await Assert.That(deserializedInput.Expression).IsNotNull();
        var value = await Assert.That(expression.Value).IsNotNull();
        await Assert.That(value).IsOfType(typeof(SecretReference));
        var deserializedReference = (SecretReference)value;

        await Assert.That(json).Contains("\"expression\":{\"type\":\"Secret\"").WithComparison(StringComparison.CurrentCulture);
        await Assert.That(json).Contains("\"value\":{\"name\":\"api:key\"").WithComparison(StringComparison.CurrentCulture);
        await Assert.That(json).DoesNotContain("top-secret").WithComparison(StringComparison.CurrentCulture);
        await Assert.That(deserializedReference).IsEqualTo(new SecretReference("api:key", SecretTypeNames.Text, "production"));
    }

    [Test]
    public async Task AddSecretsServices_RegistersSecretExpressionDescriptorProvider()
    {
        var services = new ServiceCollection();

        services.AddSecretsServices();

        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetServices<IExpressionDescriptorProvider>().Single(x => x is SecretExpressionDescriptorProvider);
        var descriptor = provider.GetDescriptors().Single();

        await Assert.That(descriptor.Type).IsEqualTo(SecretExpression.TypeName);
        await Assert.That(descriptor.Properties["UIHint"]).IsEqualTo("secret-picker");
        await Assert.That(descriptor.Properties["PickerEndpoint"]).IsEqualTo("/secrets/picker");
    }

    private async Task<T?> EvaluateAsync<T>(SecretReference reference)
    {
        var expression = SecretExpression.Create(reference);
        var context = CreateContext();
        return (T?)await _handler.EvaluateAsync(expression, typeof(T), context, ExpressionEvaluatorOptions.Empty);
    }

    private static ExpressionExecutionContext CreateContext(CancellationToken cancellationToken = default)
    {
        return new(new ServiceCollection().BuildServiceProvider(), new MemoryRegister(), cancellationToken: cancellationToken);
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var registry = new TestExpressionDescriptorRegistry(new SecretExpressionDescriptorProvider().GetDescriptors());
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IExpressionDescriptorRegistry>(registry)
            .BuildServiceProvider();

        return new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new TypeJsonConverter(),
                new ExpressionJsonConverterFactory(registry),
                new InputJsonConverterFactory(serviceProvider)
            }
        };
    }

    private class CapturingSecretResolver(string value) : ISecretResolver
    {
        public CancellationToken CancellationToken { get; private set; }

        public Task<string> ResolveAsync(string name, CancellationToken cancellationToken = default) => ResolveAsync(new SecretReference(name), cancellationToken);

        public Task<string> ResolveAsync(SecretReference reference, CancellationToken cancellationToken = default)
        {
            CancellationToken = cancellationToken;
            return Task.FromResult(value);
        }
    }

    private class TestExpressionDescriptorRegistry(IEnumerable<ExpressionDescriptor> descriptors) : IExpressionDescriptorRegistry
    {
        private readonly Dictionary<string, ExpressionDescriptor> _descriptors = descriptors.ToDictionary(x => x.Type);

        public void Add(ExpressionDescriptor descriptor) => _descriptors[descriptor.Type] = descriptor;

        public void AddRange(IEnumerable<ExpressionDescriptor> descriptors)
        {
            foreach (var descriptor in descriptors)
                Add(descriptor);
        }

        public IEnumerable<ExpressionDescriptor> ListAll() => _descriptors.Values;

        public ExpressionDescriptor? Find(Func<ExpressionDescriptor, bool> predicate) => _descriptors.Values.FirstOrDefault(predicate);

        public ExpressionDescriptor? Find(string type) => _descriptors.GetValueOrDefault(type);
    }
}
