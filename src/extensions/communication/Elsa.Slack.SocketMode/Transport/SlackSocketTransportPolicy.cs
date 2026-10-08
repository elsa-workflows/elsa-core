using System.Net;

namespace Elsa.Slack.SocketMode.Transport;

/// <summary>Production origins are fixed. The separate internal loopback policy is available only to audited fixtures.</summary>
internal sealed class SlackSocketTransportPolicy
{
    private readonly Uri? _fixtureSocket;

    private SlackSocketTransportPolicy(Uri apiEndpoint, Uri? fixtureSocket)
    {
        ApiEndpoint = apiEndpoint;
        _fixtureSocket = fixtureSocket;
    }

    internal static SlackSocketTransportPolicy Production { get; } = new(new("https://slack.com/api/apps.connections.open"), null);
    internal Uri ApiEndpoint { get; }
    internal bool IsFixture => _fixtureSocket is not null;

    internal static SlackSocketTransportPolicy ForLoopbackFixture(Uri apiEndpoint, Uri socketEndpoint)
    {
        if (apiEndpoint.Scheme != "http" || socketEndpoint.Scheme != "ws" ||
            !IsLiteralLoopback(apiEndpoint) || !IsLiteralLoopback(socketEndpoint) ||
            apiEndpoint.AbsolutePath != "/api/apps.connections.open" || apiEndpoint.Query.Length != 0 ||
            apiEndpoint.UserInfo.Length != 0 || apiEndpoint.Fragment.Length != 0 ||
            socketEndpoint.UserInfo.Length != 0 || socketEndpoint.Fragment.Length != 0 || socketEndpoint.Query.Length != 0)
        {
            throw new ArgumentException("Fixture transport requires explicit literal loopback endpoints.");
        }
        return new(apiEndpoint, socketEndpoint);
    }

    internal void ValidateSocket(Uri uri)
    {
        if (!uri.IsAbsoluteUri || uri.UserInfo.Length != 0 || uri.Fragment.Length != 0)
        {
            throw new InvalidOperationException("socket_origin_denied");
        }
        if (_fixtureSocket is not null)
        {
            if (uri.Scheme != "ws" || !IsLiteralLoopback(uri) || uri.Host != _fixtureSocket.Host ||
                uri.Port != _fixtureSocket.Port || uri.AbsolutePath != _fixtureSocket.AbsolutePath)
            {
                throw new InvalidOperationException("socket_fixture_origin_denied");
            }
            return;
        }
        // Slack's Socket Mode guide documents wss.slack.com. Its network documentation identifies the primary/backup origins.
        // https://docs.slack.dev/apis/events-api/using-socket-mode/
        // https://slack.com/help/articles/360001603387-Manage-Slack-connection-issues
        if (uri.Scheme != "wss" || uri.Port != 443 ||
            uri.IdnHost is not ("wss.slack.com" or "wss-primary.slack.com" or "wss-backup.slack.com"))
        {
            throw new InvalidOperationException("socket_origin_denied");
        }
    }

    private static bool IsLiteralLoopback(Uri uri) => IPAddress.TryParse(uri.Host.Trim('[', ']'), out var address) && IPAddress.IsLoopback(address);
}
