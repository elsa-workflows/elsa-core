# Compose the isolated Slack Socket Mode adapter

`Elsa.Slack.SocketMode` is an explicit opt-in sibling of the existing Slack package. Compose the reviewed isolated Admission host, PostgreSQL workflow/Admission persistence, Connections and managed Secrets first. The Socket feature does not select or repair those services, provision credentials, migrate databases, activate subscriptions or grant outbound workflow use. Exact-head hosted package, wire and PostgreSQL proof remains required before acceptance; registration alone is not runtime evidence.

Construct `SlackSocketModeConfiguration` with the trusted tenant/environment, stable installation and connection identities, expected app/workspace or enterprise, self user, channel, complete pinned subscription set with captured activation epochs, and every finite `SlackSocketModeLimits` value. These immutable values are server-owned; an incoming event cannot choose credentials, routes, subscriptions or definitions. Reconfiguration uses a new reviewed configuration rather than mutable listener fields.

For the classic module, configure `Elsa.Slack.SocketMode.Features.SlackSocketModeFeature` after supplying the existing Admission configuration and provider selections:

```csharp
module.Configure<Elsa.Slack.SocketMode.Features.SlackSocketModeFeature>(feature =>
{
    feature.Configuration = socketConfiguration;
    feature.ConnectionString = postgresConnectionString;
});
```

The classic feature depends on the existing Admission and Slack features. The genuine Shell feature is `Elsa.Slack.SocketMode.ShellFeatures.SlackSocketModeFeature`; select it through the normal Shell feature configuration and explicitly supply the same `Configuration` and `ConnectionString` properties. It depends on Admission's Shell feature. The legacy Slack package has no Shell feature, so the shared registration retains the same `SlackClientFactory` baseline without changing legacy token activity defaults.

Both paths call `services.AddSlackSocketMode(socketConfiguration, postgresConnectionString)` from `Elsa.Slack.SocketMode.Extensions`. Register Socket Mode once. Preexisting conflicting message sources, receipt contexts/stores or identity-conflict readers are rejected rather than silently replaced. The public helper fixes production transport origins; only internal audited fixtures can select literal loopback endpoints. No public method accepts or returns a listener bearer, principal, execution permit or mutable transport policy.

The receipt context is `SlackSocketReceiptElsaDbContext`, with fixed `Elsa` schema and its own `__SlackSocketReceiptsMigrationsHistory`. Provision migration `20261008110000_InitialSlackSocketReceipts` explicitly using the selected database deployment procedure. Admission has its separate history; Connections and Secrets retain their reviewed shared history. All four contexts must resolve the same selected PostgreSQL database, with no retrying transaction strategy or pending/model-divergent migrations. Receipt operations enlist in the Admission connection/transaction and preserve the shared retained-capacity boundary.

Startup validation checks the actual guarded host, selected stores/collision reader, immutable subscription allowlist, factory types, model/history/table layouts, provisioning and fixed listener-use authorization before transport use. Declare the actual additional EF factories and custom clock/logger/authorizer/model-handler types in the immutable Admission audit inventory. The exact `TimeProvider.System` instance is the default; retaining a custom clock registration does not exempt its concrete type from audit. The owned listener must repeat validation immediately before opening, including when hosted services start concurrently. The registration startup service performs validation only and opens no socket.

The admitted Watch mode remains an explicit `WatchPublicChannelMessageMode.AdmittedPublicMessage` selection. `LegacyToken` stays the default. Admitted data requires the exact consumed execution owner and pinned event binding; normal workflow input or a copied context cannot mint that data authority. Ingress/listener authorization does not authorize replies: existing exact-instance connection-use grants, outbound default-deny policy and remote-post uncertainty remain separate requirements. Live app/channel consent, operational policy, deployment and publication are separate gates.
