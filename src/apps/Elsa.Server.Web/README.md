# Server

This project represents an Elsa application that hosts workflows and exposes API endpoints to manage & execute workflows.

## Secrets
The following are the secrets stored in hashed form in appsettings.json:

**API key**: `48587230567A646D394B435A6277734A-4802fa49-e91e-45e8-b00f-b5492377e20b`
**Admin user**: `admin`
**Admin password**: `password`

## Ingress Rate Limiting

The reference server includes opt-in ASP.NET Core rate limiting for Elsa management API requests and public HTTP workflow trigger routes. Enable it by setting `IngressRateLimiting:Enabled` to `true`.

Default policies are intentionally conservative and queue-free:

```json
"IngressRateLimiting": {
  "Enabled": true,
  "ApiPermitLimit": 120,
  "ApiWindowSeconds": 60,
  "ApiQueueLimit": 0,
  "HttpWorkflowPermitLimit": 60,
  "HttpWorkflowWindowSeconds": 60,
  "HttpWorkflowQueueLimit": 0
}
```

Tune these values for production traffic and deployment topology. To disable the reference policies, leave `Enabled` as `false`. Custom hosts can register their own named ASP.NET Core rate limiter policies with `services.AddRateLimiter(...)`, pass the policy names through `ApiEndpointOptions.RateLimitingPolicyName` and `HttpActivityOptions.RateLimitingPolicyName`, call `UseWorkflowsApiRateLimiting(...)` and `UseWorkflowsRateLimiting(...)`, then call `app.UseRateLimiter()` once for the host pipeline. The Elsa hooks validate that configured policy names are registered and only attach endpoint metadata, so they should run before the host's single rate limiter middleware.

## OpenTelemetry (MacOS)

COR_ENABLE_PROFILING=1
COR_PROFILER={918728DD-259F-4A6A-AC2B-B85E1B658318}
CORECLR_PROFILER_PATH=$INSTALL_DIR/osx-x64/OpenTelemetry.AutoInstrumentation.Native.dylib
DOTNET_ADDITIONAL_DEPS=$INSTALL_DIR/AdditionalDeps
DOTNET_EnableDiagnostics=1
DOTNET_SHARED_STORE=$INSTALL_DIR/store
DOTNET_STARTUP_HOOKS=OpenTelemetry.AutoInstrumentation.StartupHook
OTEL_DOTNET_AUTO_HOME=$INSTALL_DIR
OTEL_DOTNET_AUTO_LOGS_CONSOLE_EXPORTER_ENABLED=true
OTEL_DOTNET_AUTO_METRICS_CONSOLE_EXPORTER_ENABLED=true
OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES=Proto.Actor,Elsa.Workflows
OTEL_DOTNET_AUTO_TRACES_CONSOLE_EXPORTER_ENABLED=true
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
OTEL_RESOURCE_ATTRIBUTES=service.name=Elsa Server,service.version=3.3.0,service.instance.id=instance-123,deployment.environment=development
