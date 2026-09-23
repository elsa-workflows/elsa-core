using System.Text.Json.Nodes;
using Bpmn.Interchange;
using Bpmn.Model;
using Elsa.Bpmn.Interchange.Endpoints.Bpmn.Document;
using Elsa.Bpmn.Interchange.Exceptions;
using Elsa.Bpmn.Interchange.Services;
using Elsa.Extensions;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Interchange;

public partial class BpmnDocumentPutCompareAndSwapTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task SqliteDocumentPut_PreservesTheConcurrentWinner(bool metadataOnly, bool published)
    {
        var directory = Path.Join(Path.GetTempPath(), $"elsa-bpmn-cas-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var connectionString = $"Data Source={Path.Join(directory, "workflows.db")};Pooling=False;Default Timeout=10";
            var probe = new DraftNotificationProbe();
            await using var services = (ServiceProvider)new TestApplicationBuilder(testOutputHelper)
                .ConfigureElsa(elsa => elsa
                    .UseBpmnInterchange()
                    .UseWorkflowManagement(management => management.UseEntityFrameworkCore(ef => ef.UseSqlite(connectionString))))
                .ConfigureServices(s =>
                {
                    s.AddSingleton(probe);
                    s.AddNotificationHandler<RejectingDraftSavingHandler>();
                })
                .Build();
            await using (var db = await services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync())
            {
                await db.Database.EnsureCreatedAsync();
            }
            await services.PopulateRegistriesAsync();
            await using var firstScope = services.CreateAsyncScope();
            await using var secondScope = services.CreateAsyncScope();
            var firstStore = firstScope.ServiceProvider.GetRequiredService<IWorkflowDefinitionStore>();
            var secondStore = secondScope.ServiceProvider.GetRequiredService<IWorkflowDefinitionStore>();
            Assert.IsType<EFCoreWorkflowDefinitionStore>(firstStore);
            Assert.IsType<EFCoreWorkflowDefinitionStore>(secondStore);
            Assert.NotSame(firstStore, secondStore);
            var setup = firstScope.ServiceProvider.GetRequiredService<BpmnInterchangeDocumentService>();
            var imported = await setup.ImportAsync(ReadAsset("camunda-order-process.bpmn"), null, "Order", null, CancellationToken.None);
            var definitionId = imported.ImportResult.WorkflowDefinition.DefinitionId;
            var initial = await FindLatestAsync(firstStore, definitionId);
            initial.IsPublished = published;
            initial.CustomProperties["test:nested"] = new JsonObject { ["value"] = "initial" };
            await firstStore.SaveAsync(initial);
            var expectedETag = BpmnDocumentETag.From(initial);
            var reader = services.GetRequiredService<BpmnXmlReader>();
            var sourceXml = (string)initial.CustomProperties[BpmnInterchangeDocumentService.SourceXmlCustomPropertyKey];
            var firstEdit = reader.Read(sourceXml.Replace("Order Handled", "Losing edit"), new BpmnImportOptions()).Definitions;
            var gate = new CompareAndSwapPauseGate();
            var pausedStore = new PausingCompareAndSwapStore(firstStore, gate);
            var firstWriter = ActivatorUtilities.CreateInstance<BpmnInterchangeDocumentService>(firstScope.ServiceProvider, pausedStore);
            var pending = firstWriter.ImportDocumentAsync(firstEdit, definitionId, null, CancellationToken.None, expectedETag);
            await gate.Checked.Task.WaitAsync(TimeSpan.FromSeconds(10));

            try
            {
                if (metadataOnly)
                {
                    var winner = await FindLatestAsync(secondStore, definitionId);
                    winner.Options.UsableAsActivity = true;
                    winner.CustomProperties["test:concurrent"] = "retained";
                    await secondStore.SaveAsync(winner);
                }
                else
                {
                    probe.ApplyDraftEdits = true;
                    var secondWriter = secondScope.ServiceProvider.GetRequiredService<BpmnInterchangeDocumentService>();
                    var secondEdit = reader.Read(sourceXml.Replace("Order Handled", "Winning edit"), new BpmnImportOptions()).Definitions;
                    var winner = await secondWriter.ImportDocumentAsync(secondEdit, definitionId, null, CancellationToken.None, expectedETag);
                    Assert.True(winner.ImportResult.Succeeded);
                }
            }
            finally
            {
                gate.Release.TrySetResult();
            }

            await Assert.ThrowsAsync<BpmnDocumentPreconditionFailedException>(() => pending);
            await using var verificationScope = services.CreateAsyncScope();
            var persisted = await FindLatestAsync(verificationScope.ServiceProvider.GetRequiredService<IWorkflowDefinitionStore>(), definitionId);
            if (metadataOnly)
            {
                Assert.True(persisted.Options.UsableAsActivity);
                Assert.Equal("retained", persisted.CustomProperties["test:concurrent"]);
                Assert.Equal(initial.StringData, persisted.StringData);
                Assert.Equal(expectedETag, BpmnDocumentETag.From(persisted));
            }
            else
            {
                Assert.Equal("Handler name", persisted.Name);
                Assert.Equal("Handler description", persisted.Description);
                Assert.True(persisted.Options.AutoUpdateConsumingWorkflows);
                Assert.Contains(persisted.Variables, variable => variable.Name == "handlerVariable");
                Assert.Equal(published ? initial.Version + 1 : initial.Version, persisted.Version);
                Assert.False(persisted.IsPublished);
                Assert.Contains("Winning edit", (string)persisted.CustomProperties[BpmnInterchangeDocumentService.SourceXmlCustomPropertyKey]);
                Assert.NotEqual(expectedETag, BpmnDocumentETag.From(persisted));
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
