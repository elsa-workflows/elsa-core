using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Slack.Features;
using Elsa.Slack.SocketMode.Extensions;
using Elsa.Workflows.Admission.Features;

namespace Elsa.Slack.SocketMode.Features;

/// <summary>Opt-in Socket ingress registration for an explicitly configured isolated Admission host.</summary>
[DependsOn(typeof(AdmissionFeature))]
[DependsOn(typeof(SlackFeature))]
public sealed class SlackSocketModeFeature(IModule module) : FeatureBase(module)
{
    public SlackSocketModeConfiguration? Configuration { get; set; }
    public string? ConnectionString { get; set; }

    public override void Apply() => Services.AddSlackSocketMode(
        Configuration ?? throw new InvalidOperationException("An explicit immutable Socket Mode configuration is required."),
        ConnectionString ?? throw new InvalidOperationException("An explicit PostgreSQL connection string is required for Socket receipts."));
}
