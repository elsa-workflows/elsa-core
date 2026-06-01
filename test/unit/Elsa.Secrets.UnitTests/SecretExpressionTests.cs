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
using Xunit;

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

    [Fact]
    public async Task EvaluateAsync_ResolvesSecretReference()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "api:key", Value = "top-secret" });

        var result = await EvaluateAsync<string>(new("api:key"));

        Assert.Equal("top-secret", result);
    }

    [Fact]
    public async Task EvaluateAsync_Throws_WhenSecretIsMissing()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => EvaluateAsync<string>(new("api:key")));

        Assert.Equal("Secret 'api:key' was not found.", exception.Message);
    }

    [Fact]
    public async Task EvaluateAsync_Throws_WhenSecretTypeDoesNotMatchReference()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "api:key", TypeName = SecretTypeNames.Text, Value = "top-secret" });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => EvaluateAsync<string>(new("api:key", SecretTypeNames.RsaKey)));

        Assert.Equal("Secret 'api:key' is not compatible with required type 'rsa-key'.", exception.Message);
    }

    [Fact]
    public async Task EvaluateAsync_Throws_WhenSecretScopeDoesNotMatchReference()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "api:key", Scope = "production", Value = "top-secret" });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => EvaluateAsync<string>(new("api:key", Scope: "development")));

        Assert.Equal("Secret 'api:key' is not compatible with required scope 'development'.", exception.Message);
    }

    [Fact]
    public async Task EvaluateAsync_PassesCancellationTokenToResolver()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var resolver = new CapturingSecretResolver("top-secret");
        var handler = new SecretExpressionHandler(resolver, _wellKnownTypeRegistry);
        var context = CreateContext(cancellationTokenSource.Token);

        await handler.EvaluateAsync(SecretExpression.Create(new("api:key")), typeof(string), context, ExpressionEvaluatorOptions.Empty);

        Assert.Equal(cancellationTokenSource.Token, resolver.CancellationToken);
    }

    [Fact]
    public void SecretExpression_RoundTripsAsSecretReference()
    {
        var options = CreateSerializerOptions();
        var expression = SecretExpression.Create(new("api:key", SecretTypeNames.Text, "production"));

        var json = JsonSerializer.Serialize(expression, options);
        var deserializedExpression = JsonSerializer.Deserialize<Expression>(json, options)!;
        var deserializedReference = Assert.IsType<SecretReference>(deserializedExpression.Value);

        Assert.Contains("\"type\":\"Secret\"", json);
        Assert.Contains("\"name\":\"api:key\"", json);
        Assert.Contains("\"typeName\":\"text\"", json);
        Assert.Contains("\"scope\":\"production\"", json);
        Assert.DoesNotContain("top-secret", json);
        Assert.Equal(SecretExpression.TypeName, deserializedExpression.Type);
        Assert.Equal(new SecretReference("api:key", SecretTypeNames.Text, "production"), deserializedReference);
    }

    [Fact]
    public void WorkflowInputJson_StoresSecretReferenceNotSecretValue()
    {
        var options = CreateSerializerOptions();
        var input = new Input<string>(SecretExpression.Create(new("api:key", SecretTypeNames.Text, "production")));

        var json = JsonSerializer.Serialize(input, options);
        var deserializedInput = JsonSerializer.Deserialize<Input<string>>(json, options)!;
        var deserializedReference = Assert.IsType<SecretReference>(deserializedInput.Expression!.Value);

        Assert.Contains("\"expression\":{\"type\":\"Secret\"", json);
        Assert.Contains("\"value\":{\"name\":\"api:key\"", json);
        Assert.DoesNotContain("top-secret", json);
        Assert.Equal(new SecretReference("api:key", SecretTypeNames.Text, "production"), deserializedReference);
    }

    [Fact]
    public void AddSecretsServices_RegistersSecretExpressionDescriptorProvider()
    {
        var services = new ServiceCollection();

        services.AddSecretsServices();

        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetServices<IExpressionDescriptorProvider>().Single(x => x is SecretExpressionDescriptorProvider);
        var descriptor = provider.GetDescriptors().Single();

        Assert.Equal(SecretExpression.TypeName, descriptor.Type);
        Assert.Equal("secret-picker", descriptor.Properties["UIHint"]);
        Assert.Equal("/secrets/picker", descriptor.Properties["PickerEndpoint"]);
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
