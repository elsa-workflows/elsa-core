"""Source contracts for passive fixture evidence; these do not execute ASP.NET.

The hosted package fixture build supplies C# compilation and real response proof.
These checks guard the fixed target, private schema, and fail-closed write surfaces.
"""
from pathlib import Path
import re
import unittest


FIXTURE = Path(__file__).parent / "paired-package-browser" / "backend" / "Program.cs"
CODE = FIXTURE.read_text()


def section(start, end):
    return CODE.split(start, 1)[1].split(end, 1)[0]


class OptionalEndpointFixtureContracts(unittest.TestCase):
    def test_canonical_context_handler_and_route_match_candidate_source(self):
        source = (Path(__file__).resolve().parents[2] /
                  "src/extensions/workflows/Elsa.WorkflowContexts/Endpoints/ProviderTypes/List/Endpoint.cs").read_text()
        self.assertIn("namespace Elsa.WorkflowContexts.Endpoints.ProviderTypes.List;", source)
        self.assertRegex(source, r"internal class List\s*:")
        self.assertIn('Get("/workflow-contexts/provider-descriptors")', source)
        self.assertIn('ConfigurePermissions("read:workflow-context-provider-descriptors")', source)
        self.assertIn('new("/elsa/api/workflow-contexts/provider-descriptors", "/workflow-contexts/provider-descriptors", "GET",\n'
                      '            "Elsa.WorkflowContexts.Endpoints.ProviderTypes.List.List", "Elsa.WorkflowContexts")', CODE)

    def test_existing_secrets_projection_remains_authenticated_and_secrets_only(self):
        self.assertIn('app.MapGet("/_fixture/secrets-endpoints", () => secretsEndpointEvidence.Snapshot()).RequireAuthorization();', CODE)
        self.assertIn("TryCreate(HttpContext context) => TryCreate(context, secretsOnly: true)", CODE)
        self.assertIn('(!secretsOnly || candidate.AssemblyName == "Elsa.Secrets")', CODE)
        self.assertIn("secretsEndpointEvidence.Record(observation with { StatusCode = context.Response.StatusCode });", CODE)
        original = section("sealed record SecretsEndpointObservation(", "static class SecretsEndpointObservationFactory")
        self.assertEqual({"route", "verb", "handler_type", "handler_assembly_name", "handler_assembly_full_name",
                          "handler_assembly_sha256", "status_code", "failure_category"},
                         set(re.findall(r'JsonPropertyName\("([a-z0-9_]+)"\)', original)))

    def test_optional_snapshot_requires_auth_and_uses_no_query_or_body_input(self):
        self.assertIn('app.MapGet("/_fixture/optional-endpoints", () => optionalEndpointEvidence.Snapshot()).RequireAuthorization();', CODE)
        collector = section("sealed class OptionalEndpointEvidenceCollector", "sealed record OptionalEndpointObservation")
        self.assertNotIn("HttpContext", collector)
        self.assertNotIn("Request", collector)
        self.assertIn("schema = 1, cursor = _cursor, truncated = _truncated", collector)
        self.assertIn("pending = _observations.Values.Count(value => value is null)", collector)
        self.assertIn(".Where(value => value is not null).ToArray()", collector)

    def test_monotonic_entry_cursor_and_bounded_pending_accounting(self):
        collector = section("sealed class OptionalEndpointEvidenceCollector", "sealed record OptionalEndpointObservation")
        begin = collector.split("public long? Begin()", 1)[1].split("public void Complete", 1)[0]
        self.assertLess(begin.index("_cursor++"), begin.index("_observations.Count >= MaximumObservations"))
        self.assertIn("private const int MaximumObservations = 64;", collector)
        self.assertIn("_truncated = true;\n                return null;", begin)
        self.assertIn("_observations.Add(_cursor, null)", begin)
        self.assertIn("!_observations.TryGetValue(sequence, out var prior) || prior is not null", collector)
        self.assertIn("_observations.OrderBy(pair => pair.Key)", collector)

    def test_identity_is_bound_to_exact_definition_and_loaded_package_assembly(self):
        factory = section("static class SecretsEndpointObservationFactory", "sealed class OptionalEndpointEvidenceCollector")
        for check in ("candidate.RequestPath == requestPath", "definition.EndpointType.FullName, target.HandlerType",
                      "definition.Routes.Length == 1", "definition.Verbs.Length == 1",
                      "routeEndpoint?.RoutePattern.RawText", "RuntimeEvidence.GetLoadedElsaAssemblyIdentity",
                      "assembly.Name != target.AssemblyName", "assembly.FullName", "assembly.Sha256"):
            self.assertIn(check, factory)

    def test_only_completion_publishes_final_status_with_abort_and_replacement_checks(self):
        middleware = section("var optionalEndpointEvidence", "app.UseAuthentication()")
        completed = middleware.split("context.Response.OnCompleted(() =>", 1)[1].split("try\n    {\n        await next();", 1)[0]
        self.assertIn("context.RequestAborted.IsCancellationRequested", completed)
        self.assertIn("!ReferenceEquals(context.Features.Get<IHttpResponseBodyFeature>(), observed)", completed)
        self.assertLess(completed.index("measurement.Finish()"), completed.index("optionalEndpointEvidence.Complete("))
        self.assertIn("StatusCode = context.Response.StatusCode", completed)
        self.assertIn('body.Complete ? null : "response_body_unobserved"', completed)
        self.assertIn("aborted.Dispose()", completed)
        self.assertIn("context.RequestAborted.Register(measurement.Fail)", middleware)
        self.assertIn("context.Response.OnStarting", middleware)
        self.assertEqual(1, middleware.count("optionalEndpointEvidence.Complete("))

    def test_private_row_schema_has_no_headers_queries_body_text_or_error_text(self):
        row = section("sealed record OptionalEndpointObservation(", "sealed class PassiveResponseBodyMeasurement")
        fields = re.findall(r'JsonPropertyName\("([a-z0-9_]+)"\)', row)
        self.assertEqual({"sequence", "endpoint", "route", "verb", "handler_type", "handler_assembly_name",
                          "handler_assembly_full_name", "handler_assembly_sha256", "status_code", "failure_category",
                          "body", "bytes", "sha256", "complete", "sensitive_items_present"}, set(fields))
        self.assertEqual(len(fields), len(set(fields)))

    def test_hashing_is_incremental_and_only_complete_zero_bytes_certify_absence(self):
        measurement = section("sealed class PassiveResponseBodyMeasurement", "sealed class PassiveResponseBodyFeature")
        self.assertIn("IncrementalHash.CreateHash(HashAlgorithmName.SHA256)", measurement)
        self.assertIn("checked(_bytes + bytes.Length)", measurement)
        self.assertIn("_hash.AppendData(bytes)", measurement)
        self.assertIn("if (!_failed)\n                    sha256 =", measurement)
        self.assertIn("return new(_bytes, sha256, !_failed, !_failed && _bytes == 0 ? false : null)", measurement)
        self.assertIn("_hash.Dispose()", measurement)
        for forbidden in ("Encoding", "JsonSerializer", "MemoryStream", "ReadAll", "ToArray(", "true : null"):
            self.assertNotIn(forbidden, measurement)

    def test_original_stream_and_writer_are_captured_before_feature_installation(self):
        feature = section("sealed class PassiveResponseBodyFeature", "sealed class PassiveResponseStream")
        self.assertIn("Stream = new PassiveResponseStream(inner.Stream, measurement)", feature)
        self.assertIn("Writer = new PassiveResponsePipeWriter(inner.Writer, measurement)", feature)
        middleware = section("var optionalEndpointEvidence", "app.UseAuthentication()")
        self.assertLess(middleware.index("new PassiveResponseBodyFeature(original, measurement)"),
                        middleware.index("context.Features.Set<IHttpResponseBodyFeature>(observed)"))
        self.assertNotIn("PipeWriter.Create", feature)

    def test_sendfile_forwards_original_parameters_and_cannot_claim_observed_bytes(self):
        sendfile = section("public async Task SendFileAsync", "sealed class PassiveResponseStream")
        self.assertLess(sendfile.index("_measurement.Fail()"), sendfile.index("await _inner.SendFileAsync(path, offset, count, cancellationToken)"))
        self.assertNotIn("Record(", sendfile)
        self.assertNotIn("File.", sendfile)
        feature = section("sealed class PassiveResponseBodyFeature", "sealed class PassiveResponseStream")
        for method in ("StartAsync(cancellationToken)", "CompleteAsync()", "DisableBuffering()"):
            self.assertIn("_inner." + method, feature)

    def test_stream_write_surfaces_forward_before_measurement_and_preserve_failures(self):
        stream = section("sealed class PassiveResponseStream", "sealed class PassiveResponsePipeWriter")
        for signature in ("void Write(byte[]", "void Write(ReadOnlySpan<byte>", "Task WriteAsync(byte[]",
                          "ValueTask WriteAsync(ReadOnlyMemory<byte>"):
            method = stream.split(signature, 1)[1].split("public override", 1)[0].split("protected override", 1)[0]
            self.assertLess(method.index("inner.Write"), method.index("measurement.Record("))
            self.assertIn("catch { measurement.Fail(); throw; }", method)
        self.assertIn("measurement.Fail();\n        inner.SetLength(value)", stream)
        self.assertIn("measurement.Fail();\n        return inner.Seek(offset, origin)", stream)

    def test_pipewriter_lease_is_hashed_before_commit_and_dropped_even_on_failure(self):
        writer = section("sealed class PassiveResponsePipeWriter", "sealed class SyntheticWorkflowContextProvider")
        advance = writer.split("public override void Advance", 1)[1].split("public override", 1)[0]
        self.assertIn("!_leased || bytes < 0 || bytes > _memory.Length", advance)
        self.assertLess(advance.index("measurement.Record(_memory.Span[..bytes], countBytes: false)"), advance.index("inner.Advance(bytes)"))
        self.assertLess(advance.index("inner.Advance(bytes)"), advance.index("measurement.CommitAdvance(bytes)"))
        self.assertIn("catch { measurement.Fail(); throw; }", advance)
        self.assertIn("finally { _memory = default; _leased = false; }", advance)
        self.assertIn("GetSpan(int sizeHint = 0) => GetMemory(sizeHint).Span", writer)
        self.assertIn("CanGetUnflushedBytes => inner.CanGetUnflushedBytes", writer)
        self.assertIn("UnflushedBytes => inner.UnflushedBytes", writer)
        write = writer.split("ValueTask<FlushResult> WriteAsync", 1)[1].split("public override", 1)[0]
        self.assertLess(write.index("await inner.WriteAsync(source, cancellationToken)"), write.index("measurement.Record(source.Span)"))
        self.assertIn("if (result.IsCanceled)", write)
        self.assertIn("inner.CancelPendingFlush()", writer)


if __name__ == "__main__":
    unittest.main()
