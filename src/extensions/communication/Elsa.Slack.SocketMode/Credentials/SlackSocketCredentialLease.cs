using Elsa.Connections.Models;
using Elsa.Connections.Services;

namespace Elsa.Slack.SocketMode.Credentials;

// This private lease is only a generation observation, never workflow execution authority.
internal sealed class SlackSocketCredentialLease(ConnectionCredentialSnapshot snapshot, string bindingFingerprint)
{
    internal ConnectionAccessCredential Credential => snapshot.Credential;
    internal long Revision => snapshot.Revision;
    internal string GenerationId => snapshot.GenerationId;
    internal string SecretName => snapshot.SecretName;
    internal string BindingFingerprint => bindingFingerprint;

    public override string ToString() => "SlackSocketCredentialLease { Redacted = true }";
}
