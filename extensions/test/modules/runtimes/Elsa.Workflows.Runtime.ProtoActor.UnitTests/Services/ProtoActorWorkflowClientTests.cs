using Elsa.Common.Multitenancy;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime.ProtoActor.Mappers;
using Elsa.Workflows.Runtime.ProtoActor.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Proto;
using Proto.Cluster;
using Proto.Cluster.Identity;
using Proto.Remote;
using Proto.Remote.GrpcNet;
using ProtoActorMappers = Elsa.Workflows.Runtime.ProtoActor.Mappers.Mappers;

namespace Elsa.Workflows.Runtime.ProtoActor.UnitTests.Services;

public class ProtoActorWorkflowClientTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InstanceExistsAsync_ReturnsStoreResultForWorkflowInstanceId(bool exists)
    {
        const string instanceId = "workflow-instance-1";
        var workflowInstanceManager = new Mock<IWorkflowInstanceManager>();
        workflowInstanceManager
            .Setup(x => x.ExistsAsync(instanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exists);

        var actorSystem = new ActorSystem()
            .WithRemote(RemoteConfig.BindToLocalhost())
            .WithCluster(ClusterConfig.Setup("test", Mock.Of<IClusterProvider>(), Mock.Of<IIdentityLookup>()));
        try
        {
            var services = new ServiceCollection();
            services.AddSingleton(actorSystem.Cluster());
            services.AddSingleton(Mock.Of<ITenantAccessor>());
            services.AddSingleton(Mock.Of<IWorkflowActivationStrategyEvaluator>());
            services.AddSingleton(workflowInstanceManager.Object);
            services.AddSingleton(Mock.Of<IWorkflowStateSerializer>());
            services.AddSingleton<ActivityHandleMapper>();
            services.AddSingleton<WorkflowDefinitionHandleMapper>();
            services.AddSingleton<ExceptionMapper>();
            services.AddSingleton<ActivityIncidentMapper>();
            services.AddSingleton<WorkflowStatusMapper>();
            services.AddSingleton<WorkflowSubStatusMapper>();
            services.AddSingleton<CreateWorkflowInstanceRequestMapper>();
            services.AddSingleton<CreateWorkflowInstanceResponseMapper>();
            services.AddSingleton<RunWorkflowInstanceRequestMapper>();
            services.AddSingleton<RunWorkflowInstanceResponseMapper>();
            services.AddSingleton<CreateAndRunWorkflowInstanceRequestMapper>();
            services.AddSingleton<RunWorkflowParamsMapper>();
            services.AddSingleton<WorkflowStateJsonMapper>();
            services.AddSingleton<ProtoActorMappers>();
            await using var serviceProvider = services.BuildServiceProvider();

            var runtime = new ProtoActorWorkflowRuntime(serviceProvider, new GuidIdentityGenerator());
            var client = await runtime.CreateClientAsync(instanceId);

            var result = await client.InstanceExistsAsync();

            Assert.Equal(exists, result);
            Assert.Equal(instanceId, client.WorkflowInstanceId);
            Assert.IsType<ProtoActorWorkflowClient>(client);
            workflowInstanceManager.Verify(x => x.ExistsAsync(instanceId, It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            await actorSystem.ShutdownAsync();
        }
    }
}
