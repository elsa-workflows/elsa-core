# HttpEndpoint Activity Component Tests

This directory contains comprehensive component tests for the `HttpEndpoint` activity class in Elsa Workflows. The tests are designed to thoroughly validate the functionality, security, and edge cases of the HttpEndpoint activity.

## Test Structure

### Test Categories

1. **Core Functionality Tests** (`HttpEndpointTests.cs`)
   - Basic HTTP endpoint functionality
   - HTTP method validation
   - Simple request/response handling
   - Concurrent request processing
   - Special character route handling
   - Synchronous workflow completion validation

2. **Route Parameters Tests** (`HttpEndpointRouteParametersTests.cs`)
   - Route parameter extraction
   - URL encoding/decoding
   - Invalid route handling

3. **Query String and Headers Tests** (`HttpEndpointQueryStringAndHeadersTests.cs`)
   - Query parameter processing
   - HTTP header extraction
   - URL encoding in query strings
   - Custom headers handling

4. **Content Processing Tests** (`HttpEndpointContentTests.cs`)
   - JSON content parsing
   - Form data handling
   - Content validation
   - Empty content scenarios

5. **File Upload Tests** (`HttpEndpointFileUploadTests.cs`)
   - Single and multiple file uploads
   - File metadata extraction
   - Mixed form data and files
   - Empty file handling

6. **Security and Edge Cases Tests** (`HttpEndpointSecurityAndEdgeCasesTests.cs`)
   - Blocked file extensions
   - File extension validation
   - Case sensitivity
   - Security constraints

### Workflow Test Fixtures

The `Workflows/` directory contains test workflow implementations:

- `BasicHttpEndpointWorkflow.cs` - Simple HTTP endpoint
- `MultipleHttpMethodsWorkflow.cs` - Multi-method endpoint with request method detection
- `RouteParametersWorkflow.cs` - Route parameter extraction
- `QueryStringAndHeadersWorkflow.cs` - Query and header processing
- `JsonContentWorkflow.cs` - JSON content parsing
- `FormDataWorkflow.cs` - Form data handling
- `FileUploadWorkflow.cs` - File upload processing
- `BlockedFileExtensionWorkflow.cs` - Security-focused workflows including authentication and blocked extensions

## Key Features Tested

### Core Functionality
- ✅ Basic HTTP endpoint creation
- ✅ Multiple HTTP methods (GET, POST, PUT, DELETE)
- ✅ Route parameter extraction with complex patterns
- ✅ Query string processing
- ✅ HTTP header extraction
- ✅ Request body parsing (JSON, form data)
- ✅ File upload handling
- ✅ Response generation

### Validation and Security
- ✅ Request size limits
- ✅ File size validation
- ✅ File extension allowlist/blocklist
- ✅ MIME type validation
- ✅ Authentication/authorization hooks
- ✅ Malformed request handling

### Edge Cases and Robustness
- ✅ Concurrent request processing
- ✅ Large payload handling
- ✅ Unicode content support
- ✅ URL encoding/decoding
- ✅ Empty and null value handling
- ✅ Case sensitivity scenarios
- ✅ Malformed multipart data

### Error Handling
- ✅ Invalid JSON processing
- ✅ Unsupported HTTP methods
- ✅ File validation failures
- ✅ Request size limit exceeded
- ✅ Missing route parameters

## Test Execution

### Prerequisites
- .NET 10.0 SDK
- PostgreSQL (for component tests)
- Docker (for TestContainers)

### Running Tests
```bash
# Run all HttpEndpoint tests
dotnet test --filter "FullyQualifiedName~HttpEndpoint"

# Run specific test categories
dotnet test --filter "HttpEndpointTests"
dotnet test --filter "HttpEndpointSecurityAndEdgeCasesTests"
dotnet test --filter "HttpEndpointRouteParametersTests"
dotnet test --filter "HttpEndpointFileUploadTests"

# Run with detailed output
dotnet test --filter "FullyQualifiedName~HttpEndpoint" --verbosity detailed
```

### Test Data and Scenarios

The tests cover a wide range of scenarios:

- **HTTP Methods**: GET, POST, PUT, DELETE, and invalid methods
- **Content Types**: JSON, form-urlencoded, multipart/form-data, plain text
- **File Types**: Text files, JSON files, binary files, empty files
- **Route Patterns**: Simple routes, parameterized routes, complex nested routes
- **Query Parameters**: Single/multiple parameters, encoded values, empty values
- **Headers**: Standard headers, custom headers, large headers
- **Request Sizes**: Small requests, large requests, oversized requests
- **Concurrent Access**: Multiple simultaneous requests
- **Unicode Support**: International characters, emojis, special symbols

## Architecture and Design

The tests follow the established patterns in the Elsa component test suite:

1. **Test Inheritance**: All tests inherit from `AppComponentTest` base class
2. **Workflow Fixtures**: Each test scenario has corresponding workflow implementations
3. **HTTP Client**: Tests use `WorkflowServer.CreateHttpWorkflowClient()` for HTTP calls
4. **Assertions**: Comprehensive assertions for status codes, content, and behavior
5. **Clean Code**: DRY principles with shared test utilities and patterns

## Integration with Elsa Framework

These tests validate integration with:

- **Workflow Runtime**: Workflow execution and lifecycle
- **HTTP Module**: HTTP request/response handling
- **Variable System**: Data flow between activities
- **Expression System**: Dynamic content generation
- **Validation Framework**: Input validation and constraints
- **Security Framework**: Authentication and authorization
- **Error Handling**: Exception handling and fault tolerance

## Continuous Integration

The tests are designed to run in CI environments and validate:

- Functional correctness
- Performance characteristics
- Security compliance
- Edge case handling
- Integration stability

## Contributing

When adding new HttpEndpoint functionality:

1. Add corresponding test workflows in `Workflows/`
2. Create test cases covering the new functionality
3. Include both positive and negative test scenarios
4. Test edge cases and error conditions
5. Ensure tests are deterministic and isolated
6. Follow existing naming and structure conventions

## Test Coverage

The test suite provides comprehensive coverage of:

- All public properties of HttpEndpoint activity
- All supported HTTP methods and content types
- Error conditions and validation scenarios
- Security constraints and edge cases
- Integration with the broader Elsa framework
- Real-world usage patterns and scenarios
