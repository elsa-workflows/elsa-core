using CShells.Features;
using Elsa.Slack.SocketMode.Extensions;
using Elsa.Workflows.Admission.ShellFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Slack.SocketMode.ShellFeatures;

/// <summary>Genuine Shell opt-in using the same fixed registration and startup checks as the classic feature.</summary>
[ShellFeature(DisplayName = "Slack Socket Mode", Description = "Guarded PostgreSQL-backed Slack Socket ingress", DependsOn = [typeof(AdmissionFeature)])]
public sealed class SlackSocketModeFeature : IShellFeature
{
    public SlackSocketModeConfiguration? Configuration { get; set; }
    public string? ConnectionString { get; set; }

    public void ConfigureServices(IServiceCollection services) => services.AddSlackSocketMode(
        Configuration ?? throw new InvalidOperationException("An explicit immutable Socket Mode configuration is required."),
        ConnectionString ?? throw new InvalidOperationException("An explicit PostgreSQL connection string is required for Socket receipts."));
}
