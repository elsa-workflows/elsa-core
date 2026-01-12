# Elsa.Http Integration Tests

This test project contains integration tests for HTTP-related activities in Elsa Workflows, specifically focusing on HTTP context loss scenarios.

## Status

✅ **Tests are working!** All tests pass successfully and are properly discovered by xUnit.

## Test Coverage

### HttpContextLossTests

Tests that verify the behavior of HTTP response activities when the HTTP context is lost during workflow execution.

#### Test Scenarios

1. **WriteHttpResponse_WithNoHttpContext_ShouldRecordIncident**
   - Verifies that `WriteHttpResponse` activity records an incident when HTTP context is null
   - Validates that the incident message clearly explains the HTTP context loss scenario
   - **Status**: ✅ Passing

2. **WriteFileHttpResponse_WithNoHttpContext_ShouldRecordIncident**
   - Verifies that `WriteFileHttpResponse` activity records an incident when HTTP context is null
   - Validates the incident message contains expected information
   - **Status**: ✅ Passing

## Expected Behavior

When HTTP context is not available:
- **Fault Code**: `NoHttpContext`
- **Fault Category**: `HTTP`
- **Fault Type**: `System`
- **Error Message**: Detailed explanation including:
  - What happened: HTTP context was lost during workflow execution
  - Why it happened: Workflow suspended and resumed in different execution context
  - Common scenarios: Background processing, virtual actor, workflow transition
  - Impact: Original HTTP request context no longer available
- **Result**: An `ActivityIncident` is recorded with the fault message

## Project Structure

```
Elsa.Http.IntegrationTests/
├── Activities/
│   ├── HttpContextLossTests.cs           # Test class using WorkflowTestFixture
│   └── Workflows/
│       ├── WriteHttpResponseWithoutHttpContextWorkflow.cs
│       └── WriteFileHttpResponseWithoutHttpContextWorkflow.cs
├── Helpers/
│   └── NullHttpContextAccessor.cs        # Mock HTTP context accessor
├── Elsa.Http.IntegrationTests.csproj
├── README.md
└── Usings.cs
```

## Running the Tests

```bash
dotnet test Elsa.Http.IntegrationTests.csproj
```

Or run specific tests:

```bash
dotnet test --filter "FullyQualifiedName~HttpContextLossTests"
```

## Test Results

```
Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2
```

## Implementation Notes

- Tests use `WorkflowTestFixture` from `Elsa.Testing.Shared` for consistent test setup
- Workflow classes are separated into individual files in the `Workflows` subfolder for better organization
- `NullHttpContextAccessor` is a test helper that simulates HTTP context loss by always returning null
- Tests automatically build the fixture and populate registries before execution

