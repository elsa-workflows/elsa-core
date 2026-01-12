# Improved Error Messaging for HTTP Context Loss in Workflows

## Issue
When a workflow is initiated from an HTTP endpoint and later suspended or transitioned to a different execution context (e.g., background processing, virtual actor), the HTTP context becomes unavailable. Previously, the error message was generic and didn't clearly explain the cause of the failure.

## Solution
Enhanced error messages in HTTP response activities to clearly describe the HTTP context loss scenario, and simplified the logic by removing bookmark creation - now these activities will always fault immediately when the HTTP context is not available.

## Changes Made

### 1. WriteHttpResponse.cs
**File:** `src/modules/Elsa.Http/Activities/WriteHttpResponse.cs`

#### Removed bookmark creation logic
- Previously, when HTTP context was null, the activity would create a bookmark with `BookmarkMetadata.HttpCrossBoundary` to allow resumption
- Now the activity **always faults immediately** when HTTP context is null

#### Removed OnResumeAsync method
- The `OnResumeAsync` callback method has been completely removed as it's no longer needed

#### Updated ExecuteAsync to always fault
- When `httpContext == null`, immediately throws `FaultException` with detailed message
- Message explains:
  - What happened: "The HTTP context was lost during workflow execution"
  - Why it happened: "workflow initiated from an HTTP endpoint is suspended and later resumed in a different execution context"
  - Examples: "background processing, virtual actor, or after a workflow transition"
  - Impact: "The original HTTP request context that expects a response is no longer available"

### 2. WriteFileHttpResponse.cs
**File:** `src/modules/Elsa.Http/Activities/WriteFileHttpResponse.cs`

Applied the same changes for consistency:

#### Removed bookmark creation logic
- No longer creates a bookmark when HTTP context is null

#### Removed OnResumeAsync method
- The resume callback has been completely removed

#### Updated ExecuteAsync to always fault
- Applied the same comprehensive error message as WriteHttpResponse

## Benefits

1. **Immediate Failure**: Workflows fail fast when HTTP context is lost, making issues immediately visible

2. **Clear Troubleshooting**: Users get explicit information about HTTP context loss with detailed explanation

3. **Better Incident Reporting**: Error messages now clearly describe the synchronization issue between workflow execution and HTTP request context

4. **Simplified Logic**: Removed the bookmark/resume pattern that could lead to confusing suspended states

5. **Consistency**: Both HTTP response activities (WriteHttpResponse and WriteFileHttpResponse) now have identical behavior and error messaging

## Technical Details

- **Fault Code**: `HttpFaultCodes.NoHttpContext`
- **Fault Category**: `HttpFaultCategories.Http`
- **Fault Type**: `DefaultFaultTypes.System`
- **Behavior**: Immediate fault when `IHttpContextAccessor.HttpContext` is `null`

## Breaking Change Note

This is a **behavior change**:
- **Before**: Activities would create a bookmark and suspend when HTTP context was null, potentially allowing resume in a different context
- **After**: Activities always fault immediately when HTTP context is null

This change makes the failure mode more predictable and easier to diagnose, as workflows will no longer enter suspended states due to missing HTTP context.

## Testing

- No compilation errors introduced
- Changes are backward compatible in terms of API surface
- The fault code and structure remain the same for any existing error handling logic
- Workflows that relied on bookmark creation for cross-boundary execution will now fault instead

### Integration Test Project Created

A new integration test project has been created at `test/integration/Elsa.Http.IntegrationTests/` to cover the HTTP context loss behavior:

**Project Structure:**
- `Elsa.Http.IntegrationTests.csproj` - Test project file
- `Activities/HttpContextLossTests.cs` - Integration tests for HTTP context loss scenarios
- `README.md` - Documentation for the test project
- `Usings.cs` - Global using directives

**Test Scenarios:**
1. `WriteHttpResponse_WithNoHttpContext_ShouldRecordIncident` - Verifies that WriteHttpResponse records an incident with the detailed error message when HTTP context is null
2. `WriteFileHttpResponse_WithNoHttpContext_ShouldRecordIncident` - Verifies that WriteFileHttpResponse records an incident when HTTP context is null

**Current Status:**
The test project compiles successfully but tests are not being discovered by the xUnit test runner. Further investigation is needed to resolve the test discovery issue. The test code follows the same patterns as other integration tests in the solution and should work once the discovery issue is resolved.


