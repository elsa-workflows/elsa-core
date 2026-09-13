using Elsa.Hosting.Management.Options;
using Elsa.Hosting.Management.Services;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading.Tasks;

namespace Elsa.Hosting.Management.UnitTests.Services;

public class ConfiguredApplicationInstanceNameProviderTests
{
    private static int ConfiguredInstanceNameMaxLength => ConfiguredApplicationInstanceNameProvider.ConfiguredInstanceNameMaxLength;

    [Test]
    public async Task ExplicitInstanceName_IsUsedDirectly()
    {
        var provider = CreateProvider(new()
        {
            InstanceName = "pod-0",
            InstanceNameEnvironmentVariable = "ELSA_TEST_INSTANCE_NAME"
        });

        await Assert.That(provider.GetName()).IsEqualTo("pod-0");
    }

    [Test]
    public async Task ExplicitInstanceName_IsTrimmed()
    {
        var provider = CreateProvider(new()
        {
            InstanceName = "  pod-0  "
        });

        await Assert.That(provider.GetName()).IsEqualTo("pod-0");
    }

    [Test]
    public async Task ExplicitInstanceName_TakesPrecedenceOverEnvironmentVariable()
    {
        var variable = NewVariableName();
        Environment.SetEnvironmentVariable(variable, "from-env");

        try
        {
            var provider = CreateProvider(new()
            {
                InstanceName = "explicit",
                InstanceNameEnvironmentVariable = variable
            });

            await Assert.That(provider.GetName()).IsEqualTo("explicit");
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, null);
        }
    }

    [Test]
    public async Task EnvironmentVariable_IsUsedWhenInstanceNameNotSet()
    {
        var variable = NewVariableName();
        Environment.SetEnvironmentVariable(variable, "pod-7");

        try
        {
            var provider = CreateProvider(new()
            {
                InstanceNameEnvironmentVariable = variable
            });

            await Assert.That(provider.GetName()).IsEqualTo("pod-7");
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, null);
        }
    }

    [Test]
    public async Task EnvironmentVariableName_IsTrimmed()
    {
        var variable = NewVariableName();
        Environment.SetEnvironmentVariable(variable, "pod-7");

        try
        {
            var provider = CreateProvider(new()
            {
                InstanceNameEnvironmentVariable = $"  {variable}  "
            });

            await Assert.That(provider.GetName()).IsEqualTo("pod-7");
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, null);
        }
    }

    [Test]
    public async Task EnvironmentVariable_ValueIsTrimmed()
    {
        var variable = NewVariableName();
        Environment.SetEnvironmentVariable(variable, "  pod-7  ");

        try
        {
            var provider = CreateProvider(new()
            {
                InstanceNameEnvironmentVariable = variable
            });

            await Assert.That(provider.GetName()).IsEqualTo("pod-7");
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, null);
        }
    }

    [Test]
    public async Task NoConfiguration_FallsBackToRandomName()
    {
        var name1 = CreateProvider(new()).GetName();
        var name2 = CreateProvider(new()).GetName();

        await Assert.That(string.IsNullOrWhiteSpace(name1)).IsFalse();
        await Assert.That(string.IsNullOrWhiteSpace(name2)).IsFalse();
        await Assert.That(name1).Matches(@"^[0-9a-f]+$");
        await Assert.That(name2).Matches(@"^[0-9a-f]+$");
        await Assert.That(name2).IsNotEqualTo(name1);
    }

    [Test]
    public async Task EnvironmentVariableConfiguredButEmpty_FallsBackToRandomName()
    {
        var variable = NewVariableName();
        Environment.SetEnvironmentVariable(variable, null);

        var name1 = CreateProvider(new() { InstanceNameEnvironmentVariable = variable }).GetName();
        var name2 = CreateProvider(new() { InstanceNameEnvironmentVariable = variable }).GetName();

        await Assert.That(string.IsNullOrWhiteSpace(name1)).IsFalse();
        await Assert.That(string.IsNullOrWhiteSpace(name2)).IsFalse();
        await Assert.That(name1).Matches(@"^[0-9a-f]+$");
        await Assert.That(name2).Matches(@"^[0-9a-f]+$");
        await Assert.That(name2).IsNotEqualTo(name1);
    }

    [Test]
    public async Task ExplicitInstanceName_AtMaximumLength_IsAccepted()
    {
        var instanceName = new string('a', ConfiguredInstanceNameMaxLength);

        var provider = CreateProvider(new() { InstanceName = instanceName });

        await Assert.That(provider.GetName()).IsEqualTo(instanceName);
    }

    [Test]
    public async Task ExplicitInstanceName_TooLong_IsShortenedDeterministically()
    {
        var instanceName = "nexxbiz-executor-api-v3-1-extra-long-replica-0001";

        var name1 = CreateProvider(new() { InstanceName = instanceName }).GetName();
        var name2 = CreateProvider(new() { InstanceName = instanceName }).GetName();

        await Assert.That(name2).IsEqualTo(name1);
        await Assert.That(name1.Length <= ConfiguredInstanceNameMaxLength).IsTrue();
        await Assert.That(name1).StartsWith(instanceName[..8]).WithComparison(StringComparison.CurrentCulture);
        await Assert.That(name1).IsNotEqualTo(instanceName);
    }

    [Test]
    [Arguments("pod 0")]
    [Arguments("pöd-0")]
    [Arguments("-pod-0")]
    [Arguments("pod-0-")]
    public async Task ExplicitInstanceName_InvalidCharacters_Throws(string instanceName)
    {
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => CreateProvider(new() { InstanceName = instanceName }));

        await Assert.That(exception.Message).Contains("contains invalid characters").WithComparison(StringComparison.CurrentCulture);
    }

    [Test]
    public async Task ExplicitInstanceName_InvalidCharactersAndTooLong_ReportsBothProblems()
    {
        var instanceName = new string('a', ConfiguredInstanceNameMaxLength) + " b";

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => CreateProvider(new() { InstanceName = instanceName }));

        await Assert.That(exception.Message).Contains("contains invalid characters").WithComparison(StringComparison.CurrentCulture);
        await Assert.That(exception.Message).Contains($"{ConfiguredInstanceNameMaxLength} characters or fewer").WithComparison(StringComparison.CurrentCulture);
        await Assert.That(exception.Message).Contains("Azure Service Bus").WithComparison(StringComparison.CurrentCulture);
    }

    [Test]
    public async Task EnvironmentVariableValue_TooLong_IsShortenedDeterministically()
    {
        var variable = NewVariableName();
        var instanceName = "nexxbiz-executor-api-v3-1-extra-long-replica-0001";
        Environment.SetEnvironmentVariable(variable, instanceName);

        try
        {
            var name1 = CreateProvider(new() { InstanceNameEnvironmentVariable = variable }).GetName();
            var name2 = CreateProvider(new() { InstanceNameEnvironmentVariable = variable }).GetName();

            await Assert.That(name2).IsEqualTo(name1);
            await Assert.That(name1.Length <= ConfiguredInstanceNameMaxLength).IsTrue();
            await Assert.That(name1).IsNotEqualTo(instanceName);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, null);
        }
    }

    private static ConfiguredApplicationInstanceNameProvider CreateProvider(ApplicationInstanceOptions options)
    {
        return new ConfiguredApplicationInstanceNameProvider(
            Microsoft.Extensions.Options.Options.Create(options),
            new RandomIntIdentityGenerator(),
            NullLogger<ConfiguredApplicationInstanceNameProvider>.Instance);
    }

    private static string NewVariableName() => "ELSA_TEST_INSTANCE_" + Guid.NewGuid().ToString("N");
}
