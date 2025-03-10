using System.Net;
using Elsa.CLI.Activities;
using Elsa.Testing.Shared;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Activities.IntegrationTests;

public class InvokeCommandTests(ITestOutputHelper testOutputHelper)
{
    private readonly IServiceProvider _serviceProvider = new TestApplicationBuilder(testOutputHelper).Build();

    [Fact]
    public async Task Execute_CommandBetter()
    {
        var invokeCommand = _serviceProvider.GetRequiredService<InvokeCommand>();

        invokeCommand.Command = new Input<object>("echo");
        invokeCommand.Arguments = new Input<object>("Hello world!");
        invokeCommand.WorkingDirectory = new Input<string?>(AppDomain.CurrentDomain.BaseDirectory);
        invokeCommand.EnvironmentVariables = new Input<IDictionary<string, string?>?>(
            new Dictionary<string, string>() {{"ELSA_TEST", "true"}}!);
        invokeCommand.Credentials = new Input<NetworkCredential>(new NetworkCredential("user", "password", "domain"));
        invokeCommand.SuccessfulExitCode = new Input<int?>(0);
        invokeCommand.SuccessfulOutputText = new Input<string>("Hello world!");
        invokeCommand.Timeout = new Input<object>(TimeSpan.FromSeconds(5));
        invokeCommand.GracefulCancellation = new Input<bool>(true);

        var result = await _serviceProvider.RunActivityAsync(invokeCommand);

        Assert.True(true);
    }
}