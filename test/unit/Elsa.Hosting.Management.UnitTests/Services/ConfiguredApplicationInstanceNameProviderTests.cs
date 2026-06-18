using Elsa.Hosting.Management.Options;
using Elsa.Hosting.Management.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.Hosting.Management.UnitTests.Services;

public class ConfiguredApplicationInstanceNameProviderTests
{
    [Fact]
    public void ExplicitInstanceName_IsUsedDirectly()
    {
        var provider = CreateProvider(new()
        {
            InstanceName = "pod-0",
            InstanceNameEnvironmentVariable = "ELSA_TEST_INSTANCE_NAME"
        });

        Assert.Equal("pod-0", provider.GetName());
    }

    [Fact]
    public void ExplicitInstanceName_IsTrimmed()
    {
        var provider = CreateProvider(new()
        {
            InstanceName = "  pod-0  "
        });

        Assert.Equal("pod-0", provider.GetName());
    }

    [Fact]
    public void ExplicitInstanceName_TakesPrecedenceOverEnvironmentVariable()
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

            Assert.Equal("explicit", provider.GetName());
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, null);
        }
    }

    [Fact]
    public void EnvironmentVariable_IsUsedWhenInstanceNameNotSet()
    {
        var variable = NewVariableName();
        Environment.SetEnvironmentVariable(variable, "pod-7");

        try
        {
            var provider = CreateProvider(new()
            {
                InstanceNameEnvironmentVariable = variable
            });

            Assert.Equal("pod-7", provider.GetName());
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, null);
        }
    }

    [Fact]
    public void EnvironmentVariable_ValueIsTrimmed()
    {
        var variable = NewVariableName();
        Environment.SetEnvironmentVariable(variable, "  pod-7  ");

        try
        {
            var provider = CreateProvider(new()
            {
                InstanceNameEnvironmentVariable = variable
            });

            Assert.Equal("pod-7", provider.GetName());
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, null);
        }
    }

    [Fact]
    public void NoConfiguration_FallsBackToRandomName()
    {
        var name1 = CreateProvider(new()).GetName();
        var name2 = CreateProvider(new()).GetName();

        Assert.False(string.IsNullOrWhiteSpace(name1));
        Assert.False(string.IsNullOrWhiteSpace(name2));
        Assert.NotEqual(name1, name2);
    }

    [Fact]
    public void EnvironmentVariableConfiguredButEmpty_FallsBackToRandomName()
    {
        var variable = NewVariableName();
        Environment.SetEnvironmentVariable(variable, null);

        var name1 = CreateProvider(new() { InstanceNameEnvironmentVariable = variable }).GetName();
        var name2 = CreateProvider(new() { InstanceNameEnvironmentVariable = variable }).GetName();

        Assert.False(string.IsNullOrWhiteSpace(name1));
        Assert.NotEqual(name1, name2);
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
