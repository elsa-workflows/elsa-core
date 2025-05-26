# Elsa Integration Testing Examples

This directory contains samples and examples showing best practices for integration testing with Elsa Workflows.

## Properly Shutting Down Elsa Background Tasks in Integration Tests

When using ASP.NET Core's `WebApplicationFactory<TStartup>` in integration tests with Elsa Workflows, it's important to properly shut down Elsa's background tasks before disposing the factory. Failure to do so can result in task crashes and errors when the test host is stopped, especially if your test drops or disposes databases.

### The Issue

Elsa uses background tasks and recurring tasks for various purposes, such as bookmark processing and workflow execution. These tasks continue running in the background, and when the test host is abruptly shut down without properly deactivating them, they may attempt to access resources (like databases) that have already been disposed or dropped, resulting in errors.

### The Solution

The `WebApplicationFactoryExtensions` class provides an extension method `ShutdownElsaAsync<TEntryPoint>` that properly shuts down all Elsa background tasks by deactivating the tenants. This should be called in your test teardown code before stopping and disposing the factory.

```csharp
// In your test's Dispose or DisposeAsync method:
await factory.ShutdownElsaAsync();
```

See the `IntegrationTestSample.cs` file for a complete example of a base test class that properly handles Elsa shutdown.

### Best Practice

Always shut down Elsa tasks explicitly before stopping and disposing your `WebApplicationFactory` in integration tests, especially if your test uses real databases that might be dropped after each test.