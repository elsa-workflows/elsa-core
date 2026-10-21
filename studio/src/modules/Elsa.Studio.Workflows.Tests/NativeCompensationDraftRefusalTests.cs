using System.Net;
using Elsa.Studio.Workflows.Domain.Extensions;
using Elsa.Studio.Workflows.Domain.Models;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public class NativeCompensationDraftRefusalTests
{
    [Fact]
    public void ParsesTheProducerShapedNativeCompensationRefusalEnvelope()
    {
        const string response = """
        {
          "errors": { "generalErrors": ["Native compensation scopes are draft-only and cannot be published or executed."] },
          "code": "NATIVE_COMPENSATION_DRAFT_ONLY",
          "data": {
            "diagnostics": [
              {
                "scopeActivityId": "scope-17",
                "stepId": "step-9",
                "role": "forward",
                "relatedActivityIds": ["forward-activity-3"],
                "mappingIds": ["capture-2"],
                "mappingId": "capture-2"
              }
            ]
          }
        }
        """;

        var validationErrors = ValidationApiExceptionExtensions.GetValidationErrorsFromContent(response);
        var refusal = NativeCompensationDraftRefusal.FromValidationErrors(validationErrors!);

        var diagnostic = Assert.Single(refusal!.Diagnostics);
        Assert.Equal("scope-17", diagnostic.ScopeActivityId);
        Assert.Equal("step-9", diagnostic.StepId);
        Assert.Equal(NativeCompensationDraftDiagnosticRole.Forward, diagnostic.Role);
        Assert.Equal(["forward-activity-3"], diagnostic.RelatedActivityIds);
        Assert.Equal(["capture-2"], diagnostic.MappingIds);
        Assert.Equal("capture-2", diagnostic.MappingId);
    }

    [Fact]
    public void PreservesStableIdentifiersExactly()
    {
        const string response = """
        {
          "errors": { "generalErrors": ["Native compensation scopes are draft-only."] },
          "code": "NATIVE_COMPENSATION_DRAFT_ONLY",
          "data": { "diagnostics": [{
            "scopeActivityId": " scope-17 ",
            "stepId": "step-9 ",
            "role": "forward",
            "relatedActivityIds": [" forward-activity-3 "],
            "mappingIds": [" capture-2 "],
            "mappingId": " capture-2 "
          }] }
        }
        """;

        var errors = ValidationApiExceptionExtensions.GetValidationErrorsFromContent(response);
        var diagnostic = Assert.Single(NativeCompensationDraftRefusal.FromValidationErrors(errors!)!.Diagnostics);

        Assert.Equal(" scope-17 ", diagnostic.ScopeActivityId);
        Assert.Equal("step-9 ", diagnostic.StepId);
        Assert.Equal([" forward-activity-3 "], diagnostic.RelatedActivityIds);
        Assert.Equal([" capture-2 "], diagnostic.MappingIds);
        Assert.Equal(" capture-2 ", diagnostic.MappingId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(", \"data\": {}")]
    [InlineData(", \"data\": { \"diagnostics\": \"untrusted\" }")]
    public void KeepsTheSafeDraftOnlyRefusalWhenDiagnosticDataIsMissingOrMalformed(string dataMember)
    {
        var response = $$"""
        {
          "errors": { "generalErrors": ["server-provided text must not be rendered"] },
          "code": "NATIVE_COMPENSATION_DRAFT_ONLY"{{dataMember}}
        }
        """;

        var validationErrors = ValidationApiExceptionExtensions.GetValidationErrorsFromContent(response);
        var refusal = NativeCompensationDraftRefusal.FromValidationErrors(validationErrors!);

        Assert.NotNull(refusal);
        Assert.Empty(refusal.Diagnostics);
    }

    [Theory]
    [InlineData("{\"code\":\"NATIVE_COMPENSATION_DRAFT_ONLY\"}")]
    [InlineData("{\"code\":\"NATIVE_COMPENSATION_DRAFT_ONLY\",\"data\":{\"diagnostics\":\"untrusted\"}}")]
    public void RecognizesCodeOnlyNativeEnvelopeWithoutValidationMessages(string response)
    {
        var errors = NativeCompensationDraftRefusal.TryGetValidationErrorsFromContent(response, HttpStatusCode.BadRequest);

        Assert.NotNull(errors);
        Assert.Equal(NativeCompensationDraftRefusal.Code, errors.Code);
        Assert.Equal(HttpStatusCode.BadRequest, errors.StatusCode);
        Assert.Empty(errors.Errors);
        Assert.Empty(NativeCompensationDraftRefusal.FromValidationErrors(errors)!.Diagnostics);
    }

    [Fact]
    public void DoesNotClaimAnUnfamiliarCodeOnlyEnvelope()
    {
        Assert.Null(NativeCompensationDraftRefusal.TryGetValidationErrorsFromContent("{\"code\":\"OTHER_FEATURE_REFUSAL\"}"));
    }

    [Fact]
    public void IgnoresWronglyTypedIdentifiersAndUnknownDiagnosticFields()
    {
        const string response = """
        {
          "errors": { "generalErrors": ["Native compensation scopes are draft-only."] },
          "code": "NATIVE_COMPENSATION_DRAFT_ONLY",
          "data": { "diagnostics": [{
            "scopeActivityId": 17,
            "stepId": { "id": "step-9" },
            "role": "forward",
            "relatedActivityIds": ["activity-1", 42],
            "mappingIds": "mapping-1",
            "mappingId": true,
            "untrustedExtra": "must not be displayed"
          }] }
        }
        """;

        var errors = ValidationApiExceptionExtensions.GetValidationErrorsFromContent(response);
        var diagnostic = Assert.Single(NativeCompensationDraftRefusal.FromValidationErrors(errors!)!.Diagnostics);

        Assert.Null(diagnostic.ScopeActivityId);
        Assert.Null(diagnostic.StepId);
        Assert.Equal(["activity-1"], diagnostic.RelatedActivityIds);
        Assert.Empty(diagnostic.MappingIds);
        Assert.Null(diagnostic.MappingId);
    }

    [Fact]
    public void DoesNotInterpretOtherCodesOrUnrecognizedRoles()
    {
        const string refusalResponse = """
        {
          "errors": { "generalErrors": ["Publish was refused."] },
          "code": "OTHER_FEATURE_REFUSAL",
          "data": { "diagnostics": [{ "scopeActivityId": "scope-17", "role": "forward" }] }
        }
        """;
        const string unknownRoleResponse = """
        {
          "errors": { "generalErrors": ["Native compensation scopes are draft-only."] },
          "code": "NATIVE_COMPENSATION_DRAFT_ONLY",
          "data": { "diagnostics": [{ "scopeActivityId": "scope-17", "role": "<script>" }] }
        }
        """;

        var otherErrors = ValidationApiExceptionExtensions.GetValidationErrorsFromContent(refusalResponse);
        var unknownRoleErrors = ValidationApiExceptionExtensions.GetValidationErrorsFromContent(unknownRoleResponse);

        Assert.Null(NativeCompensationDraftRefusal.FromValidationErrors(otherErrors!));
        Assert.Empty(NativeCompensationDraftRefusal.FromValidationErrors(unknownRoleErrors!)!.Diagnostics);
    }
}
