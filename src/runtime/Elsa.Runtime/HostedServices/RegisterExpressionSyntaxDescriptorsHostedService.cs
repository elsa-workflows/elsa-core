using Elsa.Management.Contracts;
using Microsoft.Extensions.Hosting;

namespace Elsa.Runtime.HostedServices;

public class RegisterExpressionSyntaxDescriptorsHostedService : IHostedService
{
    private readonly IExpressionSyntaxRegistryPopulator _expressionSyntaxRegistryPopulator;
    public RegisterExpressionSyntaxDescriptorsHostedService(IExpressionSyntaxRegistryPopulator expressionSyntaxRegistryPopulator) => _expressionSyntaxRegistryPopulator = expressionSyntaxRegistryPopulator;
    public async Task StartAsync(CancellationToken cancellationToken) => await _expressionSyntaxRegistryPopulator.PopulateRegistryAsync(cancellationToken);
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}