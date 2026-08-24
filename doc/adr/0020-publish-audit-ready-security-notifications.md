# Publish audit-ready security notifications

External authentication publishes immutable, redacted, typed security notifications through Elsa's existing `INotificationSender` abstraction and emits ordinary structured diagnostics separately. The module owns no audit persistence or retention policy, allowing a future audit module to subscribe, store, search, export, and optionally add durable or outbox delivery without coupling authentication to an audit database.
