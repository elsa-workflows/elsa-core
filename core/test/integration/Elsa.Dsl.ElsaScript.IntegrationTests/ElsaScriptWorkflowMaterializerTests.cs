using Elsa.Dsl.ElsaScript.Ast;
using Elsa.Dsl.ElsaScript.Contracts;
using Elsa.Dsl.ElsaScript.Materializers;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Dsl.ElsaScript.IntegrationTests;

public class ElsaScriptWorkflowMaterializerTests
{
    [Fact]
    public async Task MaterializeAsync_UsesOriginalSourceEvenWhenStringDataDiffers()
    {
        var compiler = new RecordingElsaScriptCompiler();
        var materializer = new ElsaScriptWorkflowMaterializer(compiler);
        var definition = new WorkflowDefinition
        {
            Id = "version-1",
            DefinitionId = "def-1",
            Version = 1,
            TenantId = "tenant-1",
            OriginalSource = "workflow FromFile { }",
            StringData = """{ "id": "stale-json" }"""
        };

        var workflow = await materializer.MaterializeAsync(definition, CancellationToken.None);

        Assert.Equal("workflow FromFile { }", compiler.LastSource);
        Assert.Equal(definition.DefinitionId, workflow.Identity.DefinitionId);
        Assert.Equal(definition.Version, workflow.Identity.Version);
        Assert.Equal(definition.Id, workflow.Identity.Id);
        Assert.Equal(definition.TenantId, workflow.Identity.TenantId);
    }

    private sealed class RecordingElsaScriptCompiler : IElsaScriptCompiler
    {
        public string? LastSource { get; private set; }

        public Task<Workflow> CompileAsync(string source, CancellationToken cancellationToken = default)
        {
            LastSource = source;
            return Task.FromResult(new Workflow
            {
                Identity = new("compiled", 1, "compiled")
            });
        }

        public Task<Workflow> CompileAsync(ProgramNode programNode, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
