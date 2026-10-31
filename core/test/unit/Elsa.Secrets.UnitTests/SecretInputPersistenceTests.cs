using System.Text.Json;
using Elsa.Common;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Expressions;
using Elsa.Secrets.Models;
using Elsa.Secrets.Providers;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Elsa.Secrets.UnitTests;

public class SecretInputPersistenceTests
{
    private const string SecretValue = "resolved-secret-value";

    [Fact]
    public async Task SecretBoundInput_IsNotPersistedInWorkflowStateOrJournal()
    {
        var activity = new SecretConsumer
        {
            Url = new(new Expression(SecretExpression.TypeName, new SecretReference("api:token"))),
            Body = new("public-body")
        };
        var secretResolver = Substitute.For<ISecretResolver>();
        secretResolver.ResolveAsync(Arg.Any<SecretReference>(), Arg.Any<CancellationToken>()).Returns(SecretValue);

        var fixture = new ActivityTestFixture(activity).ConfigureServices(services =>
        {
            services.AddSingleton<IExpressionDescriptorProvider, SecretExpressionDescriptorProvider>();
            services.AddSingleton(secretResolver);
        });
        var context = await fixture.BuildAsync();
        context.WorkflowExecutionContext.AddActivityExecutionContext(context);
        await fixture.ExecuteAsync(context);

        Assert.Equal(SecretValue, activity.CapturedUrl);
        Assert.Equal("public-body", context.ActivityState[nameof(SecretConsumer.Body)]);

        var workflowState = new WorkflowStateExtractor(NullLogger<WorkflowStateExtractor>.Instance).Extract(context.WorkflowExecutionContext);
        var record = await CreateMapper().MapAsync(context);

        Assert.DoesNotContain(SecretValue, JsonSerializer.Serialize(workflowState));
        Assert.False(record.ActivityState?.ContainsKey(nameof(SecretConsumer.Url)));
        Assert.DoesNotContain(SecretValue, record.SerializedSnapshot!.SerializedActivityState ?? string.Empty);
        Assert.Contains("public-body", record.SerializedSnapshot!.SerializedActivityState);
    }

    private static DefaultActivityExecutionMapper CreateMapper()
    {
        var safeSerializer = Substitute.For<ISafeSerializer>();
        safeSerializer.Serialize(Arg.Any<object>()).Returns(call => JsonSerializer.Serialize(call.Arg<object>()));
        var payloadSerializer = Substitute.For<IPayloadSerializer>();
        payloadSerializer.Serialize(Arg.Any<object>()).Returns(call => JsonSerializer.Serialize(call.Arg<object>()));
        var codec = Substitute.For<ICompressionCodec>();
        codec.CompressAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call => new ValueTask<string>(call.Arg<string>()));
        var codecResolver = Substitute.For<ICompressionCodecResolver>();
        codecResolver.Resolve(Arg.Any<string>()).Returns(codec);

        return new DefaultActivityExecutionMapper(safeSerializer, payloadSerializer, codecResolver, Microsoft.Extensions.Options.Options.Create(new ManagementOptions()));
    }

    private sealed class SecretConsumer : CodeActivity
    {
        [Input] public Input<string?> Url { get; set; } = default!;
        [Input] public Input<string?> Body { get; set; } = default!;

        public string? CapturedUrl { get; private set; }

        protected override void Execute(ActivityExecutionContext context) => CapturedUrl = context.Get(Url);
    }
}
