namespace Elsa.Testing.Shared;

/// <summary>
/// Represents a failed assertion made by a framework-neutral shared test helper.
/// </summary>
public sealed class TestAssertionException(string message) : Exception(message);
