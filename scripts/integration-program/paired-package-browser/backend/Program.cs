using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Common.Serialization;
using Elsa.Extensions;
using Elsa.Features.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Services;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Secrets.Extensions;
using Elsa.Secrets.Persistence.EFCore.Extensions;
using Elsa.Secrets.Persistence.EFCore.Sqlite.Extensions;
using Elsa.WorkflowContexts.Contracts;
using FastEndpoints;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var runtimeRoot = Path.GetFullPath(configuration["Fixture:RuntimeRoot"] ?? throw new InvalidOperationException("Missing disposable runtime root."));
var origin = configuration["Fixture:StudioOrigin"] ?? throw new InvalidOperationException("Missing Studio origin.");
var password = configuration["Fixture:Password"] ?? throw new InvalidOperationException("Missing ephemeral password.");
var passwordHash = new DefaultSecretHasher().HashSecret(password);
var permissionProfile = configuration["Fixture:PermissionProfile"];
if (permissionProfile is not ("full" or "denied" or "deny-secrets" or "deny-workflow-contexts"))
    throw new InvalidOperationException("Unknown fixture permission profile.");
var permissions = JsonSerializer.Deserialize<string[]>(configuration["Fixture:PermissionGrants"]
    ?? throw new InvalidOperationException("Missing explicit fixture permissions."));
if (permissions is not { Length: > 0 })
    throw new InvalidOperationException("Missing explicit fixture permissions.");
var contexts = configuration.GetValue<bool>("Fixture:WorkflowContexts");
var secrets = configuration.GetValue<bool>("Fixture:Secrets");

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(origin).AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("*")));
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(runtimeRoot, "keys")));
builder.Services.AddElsa(elsa =>
{
    elsa.UseIdentity(identity =>
    {
        identity.TokenOptions += options => configuration.GetSection("Identity:Tokens").Bind(options);
        identity.UseConfigurationBasedUserProvider(options => options.Users.Add(new User
        {
            Id = "paired-browser-user", Name = "paired-browser", TenantId = "",
            HashedPassword = passwordHash.EncodeSecret(), HashedPasswordSalt = passwordHash.EncodeSalt(), Roles = ["paired-browser"]
        }));
        identity.UseConfigurationBasedRoleProvider(options => options.Roles.Add(new Role
        {
            Id = "paired-browser", Name = "Paired browser fixture", TenantId = "",
            Permissions = permissions
        }));
    });
    elsa.UseDefaultAuthentication();
    elsa.UseWorkflowManagement(management => management.UseEntityFrameworkCore(ef =>
        ef.UseSqlite($"Data Source={Path.Combine(runtimeRoot, "management.db")}")));
    elsa.UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(ef =>
        ef.UseSqlite($"Data Source={Path.Combine(runtimeRoot, "runtime.db")}")));
    elsa.UseWorkflowsApi();
    elsa.UseJavaScript();
#if FIXTURE_BPMN
    elsa.UseBpmnInterchange();
#endif
    if (contexts)
    {
        elsa.UseWorkflowContexts();
    }
    if (secrets)
    {
        var encryptionKey = Convert.FromBase64String(configuration["Fixture:SecretsEncryptionKey"]
            ?? throw new InvalidOperationException("Missing ephemeral Secrets encryption key."));
        if (encryptionKey.Length != 32)
            throw new InvalidOperationException("Fixture Secrets encryption key must be 32 bytes.");
        elsa.UseSecrets(feature =>
        {
            feature.ConfigureOptions += options => options.EncryptionKey = encryptionKey;
            feature.UseEntityFrameworkCore(ef => ef.UseSqlite($"Data Source={Path.Combine(runtimeRoot, "secrets.db")}"));
        });
    }
});
builder.Services.Configure<SerializationTypeOptions>(options => options.RegisterTypeAlias(
    typeof(SyntheticWorkflowContextProvider), typeof(SyntheticWorkflowContextProvider).GetSimpleAssemblyQualifiedName()));
builder.Services.AddSingleton<IWorkflowContextProvider, SyntheticWorkflowContextProvider>();

var app = builder.Build();
var secretsEndpointEvidence = new SecretsEndpointEvidenceCollector();
var optionalEndpointEvidence = new OptionalEndpointEvidenceCollector();
app.UseCors();
app.UseRouting();
app.Use(async (context, next) =>
{
    var observation = SecretsEndpointObservationFactory.TryCreateOptional(context);
    var sequence = observation is null ? null : optionalEndpointEvidence.Begin();
    if (observation is null || sequence is null)
    {
        await next();
        return;
    }

    var measurement = new PassiveResponseBodyMeasurement();
    var original = context.Features.Get<IHttpResponseBodyFeature>();
    PassiveResponseBodyFeature? observed = null;
    try
    {
        if (original is null)
            measurement.Fail();
        else
        {
            observed = new PassiveResponseBodyFeature(original, measurement);
            context.Features.Set<IHttpResponseBodyFeature>(observed);
        }
    }
    catch
    {
        // An observer failure must not change the product response.
        measurement.Fail();
    }

    var aborted = context.RequestAborted.Register(measurement.Fail);
    context.Response.OnStarting(() =>
    {
        if (observed is null || !ReferenceEquals(context.Features.Get<IHttpResponseBodyFeature>(), observed))
            measurement.Fail();
        return Task.CompletedTask;
    });
    context.Response.OnCompleted(() =>
    {
        if (context.RequestAborted.IsCancellationRequested)
            measurement.Fail();
        if (observed is null || !ReferenceEquals(context.Features.Get<IHttpResponseBodyFeature>(), observed))
            measurement.Fail();
        var body = measurement.Finish();
        optionalEndpointEvidence.Complete(sequence.Value, observation with
        {
            StatusCode = context.Response.StatusCode,
            FailureCategory = observation.FailureCategory ?? (body.Complete ? null : "response_body_unobserved")
        }, body);
        aborted.Dispose();
        if (observed is not null && ReferenceEquals(context.Features.Get<IHttpResponseBodyFeature>(), observed))
            context.Features.Set<IHttpResponseBodyFeature>(original);
        return Task.CompletedTask;
    });
    try
    {
        await next();
    }
    catch
    {
        measurement.Fail();
        throw;
    }
});
app.Use(async (context, next) =>
{
    var observation = SecretsEndpointObservationFactory.TryCreate(context);
    if (observation is null)
    {
        await next();
        return;
    }

    await next();
    secretsEndpointEvidence.Record(observation with { StatusCode = context.Response.StatusCode });
});
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi();
app.MapGet("/_fixture/ready", () => new
{
    schema = 1, framework = AppContext.TargetFrameworkName, runtime = RuntimeInformation.FrameworkDescription,
    auth_mode = "ElsaIdentity", permission_profile = permissionProfile, permission_grants = permissions,
    workflow_contexts_enabled = contexts, secrets_enabled = secrets,
    features = app.Services.GetRequiredService<IInstalledFeatureProvider>().List().Select(feature => feature.FullName)
});
app.MapGet("/_fixture/assemblies", () => RuntimeEvidence.LoadedAssemblies()).RequireAuthorization();
app.MapGet("/_fixture/secrets-endpoints", () => secretsEndpointEvidence.Snapshot()).RequireAuthorization();
app.MapGet("/_fixture/optional-endpoints", () => optionalEndpointEvidence.Snapshot()).RequireAuthorization();
await app.RunAsync();

sealed class SecretsEndpointEvidenceCollector
{
    private const int MaximumObservations = 64;
    private readonly object _gate = new();
    private readonly List<SecretsEndpointObservation> _observations = [];
    private bool _truncated;

    public void Record(SecretsEndpointObservation observation)
    {
        lock (_gate)
        {
            if (_observations.Count >= MaximumObservations)
            {
                _truncated = true;
                return;
            }

            _observations.Add(observation);
        }
    }

    public object Snapshot()
    {
        lock (_gate)
            return new { schema = 1, truncated = _truncated, observations = _observations.ToArray() };
    }
}

sealed record SecretsEndpointObservation(
    [property: JsonPropertyName("route")] string Route,
    [property: JsonPropertyName("verb")] string Verb,
    [property: JsonPropertyName("handler_type")] string? HandlerType,
    [property: JsonPropertyName("handler_assembly_name")] string? HandlerAssemblyName,
    [property: JsonPropertyName("handler_assembly_full_name")] string? HandlerAssemblyFullName,
    [property: JsonPropertyName("handler_assembly_sha256")] string? HandlerAssemblySha256,
    [property: JsonPropertyName("status_code")] int? StatusCode,
    [property: JsonPropertyName("failure_category")] string? FailureCategory);

static class SecretsEndpointObservationFactory
{
    private sealed record Target(string RequestPath, string DefinitionRoute, string Verb, string HandlerType, string AssemblyName);

    private static readonly Target[] Targets =
    [
        new("/elsa/api/secrets/descriptors", "/secrets/descriptors", "GET",
            "Elsa.Secrets.Endpoints.Secrets.Descriptors.Endpoint", "Elsa.Secrets"),
        new("/elsa/api/secrets/picker", "/secrets/picker", "POST",
            "Elsa.Secrets.Endpoints.Secrets.Picker.Endpoint", "Elsa.Secrets"),
        // Candidate ba5b348: Endpoints/ProviderTypes/List/Endpoint.cs declares class List.
        new("/elsa/api/workflow-contexts/provider-descriptors", "/workflow-contexts/provider-descriptors", "GET",
            "Elsa.WorkflowContexts.Endpoints.ProviderTypes.List.List", "Elsa.WorkflowContexts")
    ];

    public static SecretsEndpointObservation? TryCreate(HttpContext context) => TryCreate(context, secretsOnly: true);

    public static SecretsEndpointObservation? TryCreateOptional(HttpContext context) => TryCreate(context, secretsOnly: false);

    private static SecretsEndpointObservation? TryCreate(HttpContext context, bool secretsOnly)
    {
        var requestPath = NormalizePath(context.Request.Path.Value);
        var target = Targets.SingleOrDefault(candidate =>
            (!secretsOnly || candidate.AssemblyName == "Elsa.Secrets") &&
            candidate.RequestPath == requestPath &&
            string.Equals(candidate.Verb, context.Request.Method, StringComparison.OrdinalIgnoreCase));
        if (target is null)
            return null;

        var endpoint = context.GetEndpoint();
        var definition = endpoint?.Metadata.GetMetadata<EndpointDefinition>();
        var routeEndpoint = endpoint as RouteEndpoint;
        var definitionMatches = definition is not null &&
            string.Equals(definition.EndpointType.FullName, target.HandlerType, StringComparison.Ordinal) &&
            definition.Routes.Length == 1 &&
            string.Equals(NormalizePath(definition.Routes[0]), target.DefinitionRoute, StringComparison.Ordinal) &&
            definition.Verbs.Length == 1 &&
            string.Equals(definition.Verbs[0], target.Verb, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(NormalizePath(routeEndpoint?.RoutePattern.RawText), target.RequestPath, StringComparison.Ordinal);

        if (!definitionMatches)
        {
            return new SecretsEndpointObservation(
                target.RequestPath, target.Verb, null, null, null, null, null,
                definition is null ? "endpoint_definition_missing" : "endpoint_definition_mismatch");
        }

        var assembly = RuntimeEvidence.GetLoadedElsaAssemblyIdentity(definition!.EndpointType.Assembly);
        if (assembly is null || assembly.Name != target.AssemblyName)
        {
            return new SecretsEndpointObservation(
                target.RequestPath, target.Verb, null, null, null, null, null,
                "handler_assembly_not_canonical");
        }

        return new SecretsEndpointObservation(
            target.RequestPath,
            target.Verb,
            definition.EndpointType.FullName,
            assembly.Name,
            assembly.FullName,
            assembly.Sha256,
            null,
            null);
    }

    private static string NormalizePath(string? path) =>
        string.IsNullOrWhiteSpace(path) ? "/" : $"/{path.Trim().Trim('/')}";
}

// Passive entity-byte evidence only: no body, headers, query or error text is retained.
// Sequence is assigned at entry. A snapshot with pending != 0 is not an action boundary.
sealed class OptionalEndpointEvidenceCollector
{
    private const int MaximumObservations = 64;
    private readonly object _gate = new();
    private readonly Dictionary<long, OptionalEndpointObservation?> _observations = [];
    private long _cursor;
    private bool _truncated;

    public long? Begin()
    {
        lock (_gate)
        {
            _cursor++;
            if (_observations.Count >= MaximumObservations)
            {
                _truncated = true;
                return null;
            }
            _observations.Add(_cursor, null);
            return _cursor;
        }
    }

    public void Complete(long sequence, SecretsEndpointObservation endpoint, PassiveResponseBodyObservation body)
    {
        lock (_gate)
        {
            if (!_observations.TryGetValue(sequence, out var prior) || prior is not null)
            {
                _truncated = true;
                return;
            }
            var name = endpoint.Route switch
            {
                "/elsa/api/secrets/descriptors" => "secrets-descriptors",
                "/elsa/api/secrets/picker" => "secrets-picker",
                "/elsa/api/workflow-contexts/provider-descriptors" => "workflow-context-descriptors",
                _ => throw new InvalidOperationException("Unknown optional endpoint.")
            };
            _observations[sequence] = new OptionalEndpointObservation(sequence, name, endpoint.Route, endpoint.Verb,
                endpoint.HandlerType, endpoint.HandlerAssemblyName, endpoint.HandlerAssemblyFullName,
                endpoint.HandlerAssemblySha256, endpoint.StatusCode, endpoint.FailureCategory, body);
        }
    }

    public object Snapshot()
    {
        lock (_gate)
            return new
            {
                schema = 1, cursor = _cursor, truncated = _truncated,
                pending = _observations.Values.Count(value => value is null),
                observations = _observations.OrderBy(pair => pair.Key).Select(pair => pair.Value)
                    .Where(value => value is not null).ToArray()
            };
    }
}

sealed record OptionalEndpointObservation(
    [property: JsonPropertyName("sequence")] long Sequence,
    [property: JsonPropertyName("endpoint")] string Endpoint,
    [property: JsonPropertyName("route")] string Route,
    [property: JsonPropertyName("verb")] string Verb,
    [property: JsonPropertyName("handler_type")] string? HandlerType,
    [property: JsonPropertyName("handler_assembly_name")] string? HandlerAssemblyName,
    [property: JsonPropertyName("handler_assembly_full_name")] string? HandlerAssemblyFullName,
    [property: JsonPropertyName("handler_assembly_sha256")] string? HandlerAssemblySha256,
    [property: JsonPropertyName("status_code")] int? StatusCode,
    [property: JsonPropertyName("failure_category")] string? FailureCategory,
    [property: JsonPropertyName("body")] PassiveResponseBodyObservation Body);

sealed record PassiveResponseBodyObservation(
    [property: JsonPropertyName("bytes")] long Bytes,
    [property: JsonPropertyName("sha256")] string? Sha256,
    [property: JsonPropertyName("complete")] bool Complete,
    [property: JsonPropertyName("sensitive_items_present")] bool? SensitiveItemsPresent);

sealed class PassiveResponseBodyMeasurement
{
    private readonly object _gate = new();
    private readonly IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    private long _bytes;
    private bool _failed;
    private bool _finished;

    public void Fail()
    {
        lock (_gate)
            _failed = true;
    }

    public void Record(ReadOnlySpan<byte> bytes, bool countBytes = true)
    {
        lock (_gate)
        {
            if (_finished)
            {
                _failed = true;
                return;
            }
            try
            {
                if (countBytes)
                    _bytes = checked(_bytes + bytes.Length);
                _hash.AppendData(bytes);
            }
            catch
            {
                _failed = true;
            }
        }
    }

    public void CommitAdvance(int bytes)
    {
        lock (_gate)
        {
            if (_finished || bytes < 0)
            {
                _failed = true;
                return;
            }
            try { _bytes = checked(_bytes + bytes); }
            catch { _failed = true; }
        }
    }

    public PassiveResponseBodyObservation Finish()
    {
        lock (_gate)
        {
            if (_finished)
                return new(_bytes, null, false, null);
            _finished = true;
            string? sha256 = null;
            try
            {
                if (!_failed)
                    sha256 = Convert.ToHexString(_hash.GetHashAndReset()).ToLowerInvariant();
            }
            catch
            {
                _failed = true;
            }
            finally
            {
                _hash.Dispose();
            }
            return new(_bytes, sha256, !_failed, !_failed && _bytes == 0 ? false : null);
        }
    }
}

sealed class PassiveResponseBodyFeature : IHttpResponseBodyFeature
{
    private readonly IHttpResponseBodyFeature _inner;
    private readonly PassiveResponseBodyMeasurement _measurement;

    public PassiveResponseBodyFeature(IHttpResponseBodyFeature inner, PassiveResponseBodyMeasurement measurement)
    {
        _inner = inner;
        _measurement = measurement;
        // Capture both original surfaces before installation. Neither delegates to our wrapper,
        // so a PipeWriter flush through its original Stream cannot be counted a second time.
        Stream = new PassiveResponseStream(inner.Stream, measurement);
        Writer = new PassiveResponsePipeWriter(inner.Writer, measurement);
    }

    public Stream Stream { get; }
    public PipeWriter Writer { get; }
    public void DisableBuffering()
    {
        try { _inner.DisableBuffering(); }
        catch { _measurement.Fail(); throw; }
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        try { await _inner.StartAsync(cancellationToken); }
        catch { _measurement.Fail(); throw; }
    }

    public async Task CompleteAsync()
    {
        try { await _inner.CompleteAsync(); }
        catch { _measurement.Fail(); throw; }
    }

    public async Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default)
    {
        // Native sendfile bypasses both observable write surfaces. Forward unchanged;
        // never read the file to manufacture a body hash or retain its path.
        _measurement.Fail();
        await _inner.SendFileAsync(path, offset, count, cancellationToken);
    }
}

sealed class PassiveResponseStream(Stream inner, PassiveResponseBodyMeasurement measurement) : Stream
{
    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => inner.CanSeek;
    public override bool CanWrite => inner.CanWrite;
    public override bool CanTimeout => inner.CanTimeout;
    public override int ReadTimeout { get => inner.ReadTimeout; set => inner.ReadTimeout = value; }
    public override int WriteTimeout { get => inner.WriteTimeout; set => inner.WriteTimeout = value; }
    public override long Length => inner.Length;
    public override long Position
    {
        get => inner.Position;
        set { measurement.Fail(); inner.Position = value; }
    }
    public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
    public override long Seek(long offset, SeekOrigin origin)
    {
        measurement.Fail();
        return inner.Seek(offset, origin);
    }
    public override void SetLength(long value)
    {
        measurement.Fail();
        inner.SetLength(value);
    }
    public override void Flush()
    {
        try { inner.Flush(); }
        catch { measurement.Fail(); throw; }
    }
    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        try { await inner.FlushAsync(cancellationToken); }
        catch { measurement.Fail(); throw; }
    }
    public override void Write(byte[] buffer, int offset, int count)
    {
        try { inner.Write(buffer, offset, count); measurement.Record(buffer.AsSpan(offset, count)); }
        catch { measurement.Fail(); throw; }
    }
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        try { inner.Write(buffer); measurement.Record(buffer); }
        catch { measurement.Fail(); throw; }
    }
    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        try
        {
            await inner.WriteAsync(buffer, offset, count, cancellationToken);
            measurement.Record(buffer.AsSpan(offset, count));
        }
        catch { measurement.Fail(); throw; }
    }
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        try { await inner.WriteAsync(buffer, cancellationToken); measurement.Record(buffer.Span); }
        catch { measurement.Fail(); throw; }
    }
    protected override void Dispose(bool disposing)
    {
        try
        {
            if (disposing)
                inner.Dispose();
        }
        catch { measurement.Fail(); throw; }
        base.Dispose(disposing);
    }
    public override async ValueTask DisposeAsync()
    {
        try { await inner.DisposeAsync(); }
        catch { measurement.Fail(); throw; }
    }
}

sealed class PassiveResponsePipeWriter(PipeWriter inner, PassiveResponseBodyMeasurement measurement) : PipeWriter
{
    private Memory<byte> _memory;
    private bool _leased;

    public override bool CanGetUnflushedBytes => inner.CanGetUnflushedBytes;
    public override long UnflushedBytes => inner.UnflushedBytes;

    public override Memory<byte> GetMemory(int sizeHint = 0)
    {
        try
        {
            _memory = inner.GetMemory(sizeHint);
            _leased = true;
            return _memory;
        }
        catch { measurement.Fail(); throw; }
    }
    // Keep a Memory view of the same original lease so Advance can hash without
    // copying or retaining a response buffer after the lease is committed.
    public override Span<byte> GetSpan(int sizeHint = 0) => GetMemory(sizeHint).Span;
    public override void Advance(int bytes)
    {
        try
        {
            if (!_leased || bytes < 0 || bytes > _memory.Length)
                measurement.Fail();
            else
                measurement.Record(_memory.Span[..bytes], countBytes: false);
            inner.Advance(bytes);
            measurement.CommitAdvance(bytes);
        }
        catch { measurement.Fail(); throw; }
        finally { _memory = default; _leased = false; }
    }
    public override async ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await inner.FlushAsync(cancellationToken);
            if (result.IsCanceled)
                measurement.Fail();
            return result;
        }
        catch { measurement.Fail(); throw; }
    }
    public override async ValueTask<FlushResult> WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await inner.WriteAsync(source, cancellationToken);
            measurement.Record(source.Span);
            if (result.IsCanceled)
                measurement.Fail();
            return result;
        }
        catch { measurement.Fail(); throw; }
        finally { _memory = default; _leased = false; }
    }
    public override void CancelPendingFlush()
    {
        try { inner.CancelPendingFlush(); }
        catch { measurement.Fail(); throw; }
    }
    public override void Complete(Exception? exception = null)
    {
        if (exception is not null)
            measurement.Fail();
        try { inner.Complete(exception); }
        catch { measurement.Fail(); throw; }
        finally { _memory = default; _leased = false; }
    }
    public override async ValueTask CompleteAsync(Exception? exception = null)
    {
        if (exception is not null)
            measurement.Fail();
        try { await inner.CompleteAsync(exception); }
        catch { measurement.Fail(); throw; }
        finally { _memory = default; _leased = false; }
    }
}

sealed class SyntheticWorkflowContextProvider : IWorkflowContextProvider
{
    public ValueTask<object?> LoadAsync(Elsa.Workflows.WorkflowExecutionContext context) => new((object?)"synthetic");
    public ValueTask SaveAsync(Elsa.Workflows.WorkflowExecutionContext context, object? value) => ValueTask.CompletedTask;
}
