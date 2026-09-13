using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.JavaScript.TypeDefinitions.Contracts;
using Elsa.Expressions.JavaScript.TypeDefinitions.Models;
using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.JavaScript.IntegrationTests;

public class SecretsJavaScriptTests : IAsyncDisposable
{
    private readonly TestSecretResolver _secretResolver = new();
    private readonly WorkflowTestFixture _fixture;

    public SecretsJavaScriptTests()
    {
        _fixture = new(TestContext.Current!.Output.StandardOutput);
        _fixture
            .ConfigureServices(services => services.AddSingleton<ISecretResolver>(_secretResolver))
            .ConfigureElsa(ConfigureElsa);
    }

    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Test]
    public async Task GetSecret_ReturnsPromiseCompatibleSecretValue()
    {
        var result = await EvaluateScriptAsync<string>("return getSecret('api:key');");

        await Assert.That(result).IsEqualTo("secret-value");
        await Assert.That(_secretResolver.ResolvedNames).IsEquivalentTo(
            ["api:key"],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task GetSecret_ComposesWithThen()
    {
        var result = await EvaluateScriptAsync<string>("return getSecret('api:key').then(value => value + '-suffix');");

        await Assert.That(result).IsEqualTo("secret-value-suffix");
    }

    [Test]
    public async Task GetSecret_ComposesWithAsyncIife()
    {
        var result = await EvaluateScriptAsync<string>("return (async () => await getSecret('api:key'))();");

        await Assert.That(result).IsEqualTo("secret-value");
    }

    [Test]
    public async Task TypeDefinitions_DocumentGetSecretAsPromiseOfString()
    {
        var typeDefinitions = await GenerateTypeDefinitionsAsync();

        await Assert.That(typeDefinitions).Contains("declare function getSecret(name: string): Promise<string>;", StringComparison.CurrentCulture);
    }

    private static void ConfigureElsa(IModule module)
    {
        module.UseSecretsJavaScript();
    }

    private async Task<T> EvaluateScriptAsync<T>(string script)
    {
        var context = await _fixture.CreateExpressionExecutionContextAsync();
        var evaluator = _fixture.Services.GetRequiredService<IJavaScriptEvaluator>();
        var result = await evaluator.EvaluateAsync(script, typeof(T), context);

        return result is T typedResult ? typedResult : (T)Convert.ChangeType(result, typeof(T))!;
    }

    private async Task<string> GenerateTypeDefinitionsAsync()
    {
        await _fixture.BuildAsync();
        var workflow = new Workflow();
        var workflowGraphBuilder = _fixture.Services.GetRequiredService<IWorkflowGraphBuilder>();
        var workflowGraph = await workflowGraphBuilder.BuildAsync(workflow);
        var typeDefinitionContext = new TypeDefinitionContext(workflowGraph, null, null, CancellationToken.None);
        var typeDefinitionService = _fixture.Services.GetRequiredService<ITypeDefinitionService>();

        return await typeDefinitionService.GenerateTypeDefinitionsAsync(typeDefinitionContext);
    }

    private class TestSecretResolver : ISecretResolver
    {
        private readonly Dictionary<string, string> _secrets = new(StringComparer.OrdinalIgnoreCase)
        {
            ["api:key"] = "secret-value"
        };

        public List<string> ResolvedNames { get; } = [];

        public Task<string> ResolveAsync(string name, CancellationToken cancellationToken = default)
        {
            ResolvedNames.Add(name);
            return Task.FromResult(_secrets[name]);
        }

        public Task<string> ResolveAsync(SecretReference reference, CancellationToken cancellationToken = default)
        {
            return ResolveAsync(reference.Name, cancellationToken);
        }
    }
}
