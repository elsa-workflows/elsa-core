using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Expressions.Helpers;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Threading.Tasks;

namespace Elsa.Workflows.Core.UnitTests.VariableStorageDrivers;

public class WorkflowInstanceStorageDriverTests
{
    private const string IsolatedTestMarker = "ELSA_WORKFLOW_INSTANCE_STORAGE_DRIVER_ISOLATED_TEST";

    [Test]
    public async Task WriteAsync_WhenSerializeFails_PreservesPreviousValue()
    {
        var harness = CreateHarness(new Variable<string>("name", "kept"));
        const string id = "nameVariable";

        await harness.Driver.WriteAsync(id, "kept", harness.Context);

        var storedBefore = GetVariables(harness.Properties)[id].ToJsonString();

        await harness.Driver.WriteAsync(id, CreateUnserializableValue(), harness.Context);

        var dictionary = GetVariables(harness.Properties);
        await Assert.That(dictionary.ContainsKey(id)).IsTrue();
        await Assert.That(dictionary[id].ToJsonString()).IsEqualTo(storedBefore);

        var read = await harness.Driver.ReadAsync(id, harness.Context);
        await Assert.That(read).IsEqualTo("kept");
    }

    [Test]
    public async Task WriteAsync_WhenSerializeFailsWithNoPriorValue_DoesNotCreateEntry()
    {
        var harness = CreateHarness(new Variable<string>("name", ""));
        const string id = "nameVariable";

        await harness.Driver.WriteAsync(id, CreateUnserializableValue(), harness.Context);

        await Assert.That(GetVariables(harness.Properties).ContainsKey(id)).IsFalse();
    }

    [Test]
    public async Task DeleteAsync_RemovesStoredValue()
    {
        var harness = CreateHarness(new Variable<string>("name", "kept"));
        const string id = "nameVariable";

        await harness.Driver.WriteAsync(id, "kept", harness.Context);
        await harness.Driver.DeleteAsync(id, harness.Context);

        await Assert.That(GetVariables(harness.Properties).ContainsKey(id)).IsFalse();
    }

    [Test]
    public async Task ReadAsync_WhenConvertFails_DoesNotReturnUntypedJsonNode()
    {
        var harness = CreateHarness(new Variable<int>("count", 0));
        const string id = "countVariable";
        SeedIncompatibleNode(harness.Properties, id);

        var read = await harness.Driver.ReadAsync(id, harness.Context);

        await Assert.That(read).IsNull();
        await Assert.That(read is JsonNode).IsFalse();
        await Assert.That(GetVariables(harness.Properties).ContainsKey(id)).IsTrue();
    }

    [Test]
    public async Task ReadAsync_WhenConvertFailsAndStrictMode_Throws()
    {
        const string testName = nameof(ReadAsync_WhenConvertFailsAndStrictMode_Throws);
        if (!string.Equals(Environment.GetEnvironmentVariable(IsolatedTestMarker), testName, StringComparison.Ordinal))
        {
            await RunIsolatedTestAsync(testName);
            return;
        }

        var harness = CreateHarness(new Variable<int>("count", 0));
        const string id = "countVariable";
        SeedIncompatibleNode(harness.Properties, id);

        var originalStrictMode = ObjectConverter.StrictMode;
        try
        {
            ObjectConverter.StrictMode = true;

            await Assert.That(() => harness.Driver.ReadAsync(id, harness.Context).AsTask()).Throws<Exception>();

            await Assert.That(GetVariables(harness.Properties).ContainsKey(id)).IsTrue();
        }
        finally
        {
            ObjectConverter.StrictMode = originalStrictMode;
        }
    }

    private static async Task RunIsolatedTestAsync(string testName)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(typeof(WorkflowInstanceStorageDriverTests).Assembly.Location);
        startInfo.ArgumentList.Add("--treenode-filter");
        startInfo.ArgumentList.Add($"/*/*/{nameof(WorkflowInstanceStorageDriverTests)}/{testName}");
        startInfo.Environment[IsolatedTestMarker] = testName;

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
            throw new InvalidOperationException($"Could not start isolated test process for {testName}.");

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;

        await Assert.That(process.ExitCode)
            .IsEqualTo(0)
            .Because($"Isolated test process for {testName} failed.{Environment.NewLine}{standardOutput}{Environment.NewLine}{standardError}");
    }

    private static Harness CreateHarness(Variable variable)
    {
        var properties = new Dictionary<string, object>();
        var executionContext = Substitute.For<IExecutionContext>();
        executionContext.Properties.Returns(properties);

        var payloadSerializer = Substitute.For<IPayloadSerializer>();
        payloadSerializer.GetOptions().Returns(new JsonSerializerOptions());

        var driver = new WorkflowInstanceStorageDriver(payloadSerializer, NullLogger<WorkflowInstanceStorageDriver>.Instance);
        var context = new StorageDriverContext(executionContext, variable, CancellationToken.None);

        return new(driver, context, properties);
    }

    private static VariablesDictionary GetVariables(IDictionary<string, object> properties) =>
        (VariablesDictionary)properties[WorkflowInstanceStorageDriver.VariablesDictionaryStateKey];

    private static void SeedIncompatibleNode(IDictionary<string, object> properties, string id)
    {
        properties[WorkflowInstanceStorageDriver.VariablesDictionaryStateKey] = new VariablesDictionary
        {
            [id] = JsonNode.Parse("""{"foo":"bar"}""")!
        };
    }

    private static CyclicValue CreateUnserializableValue()
    {
        var value = new CyclicValue();
        value.Self = value;
        return value;
    }

    private sealed record Harness(
        WorkflowInstanceStorageDriver Driver,
        StorageDriverContext Context,
        IDictionary<string, object> Properties);

    private sealed class CyclicValue
    {
        public CyclicValue Self { get; set; } = null!;
    }
}
