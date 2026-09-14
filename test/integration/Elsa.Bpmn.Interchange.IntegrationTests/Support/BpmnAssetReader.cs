namespace Elsa.Bpmn.Interchange.IntegrationTests.Support;

/// <summary>
/// Reads a fixture from the <c>Assets</c> directory shipped alongside this test project — the one place every test
/// that needs a BPMN document body goes through, rather than each duplicating its own <see cref="Path.Combine"/> call.
/// </summary>
internal static class BpmnAssetReader
{
    /// <summary>Reads a fixture from the <c>Assets</c> directory shipped alongside this test project.</summary>
    /// <param name="fileName">A plain file name (no directory segments) of a fixture under <c>Assets</c>.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="fileName"/> is rooted or contains a path segment, which could otherwise resolve outside the
    /// <c>Assets</c> directory. The guard below makes this defence-in-depth rather than the only thing standing
    /// between the code and a wrong path: <see cref="Path.Join(string,string,string)"/> — used instead of
    /// <see cref="Path.Combine(string,string,string)"/> below — does not discard earlier segments when a later one
    /// looks rooted.
    /// </exception>
    public static string Read(string fileName)
    {
        if (Path.IsPathRooted(fileName) || fileName.Split('/', '\\').Length > 1)
            throw new ArgumentException($"'{fileName}' must be a plain file name, not a rooted or nested path.", nameof(fileName));

        return File.ReadAllText(Path.Join(AppContext.BaseDirectory, "Assets", fileName));
    }
}
