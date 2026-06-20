using Elsa.Features.Services;
using Elsa.Hosting.Management.Contracts;
using Elsa.Hosting.Management.Options;
using Elsa.Hosting.Management.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ClusteringFeature = Elsa.Hosting.Management.Features.ClusteringFeature;
using ShellClusteringFeature = Elsa.Hosting.Management.ShellFeatures.ClusteringFeature;

namespace Elsa.Hosting.Management.UnitTests.Services;

public class ConfiguredApplicationInstanceNameProviderTests
{
    private static int AzureServiceBusSubscriptionNameMaxLength => ConfiguredApplicationInstanceNameProvider.AzureServiceBusSubscriptionNameMaxLength;
    private static string TriggerChangeTokenSignalEndpointNameSuffix => ConfiguredApplicationInstanceNameProvider.TriggerChangeTokenSignalEndpointNameSuffix;
    private static int ConfiguredInstanceNameMaxLength => ConfiguredApplicationInstanceNameProvider.ConfiguredInstanceNameMaxLength;

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
    public void EnvironmentVariableName_IsTrimmed()
    {
        var variable = NewVariableName();
        Environment.SetEnvironmentVariable(variable, "pod-7");

        try
        {
            var provider = CreateProvider(new()
            {
                InstanceNameEnvironmentVariable = $"  {variable}  "
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

    [Fact]
    public void ExplicitInstanceName_AtMaximumLength_IsAccepted()
    {
        var instanceName = new string('a', ConfiguredInstanceNameMaxLength);

        var provider = CreateProvider(new() { InstanceName = instanceName });

        Assert.Equal(instanceName, provider.GetName());
    }

    [Fact]
    public void ExplicitInstanceName_TooLong_IsShortenedDeterministically()
    {
        var instanceName = "nexxbiz-executor-api-v3-1-extra-long-replica-0001";

        var name1 = CreateProvider(new() { InstanceName = instanceName }).GetName();
        var name2 = CreateProvider(new() { InstanceName = instanceName }).GetName();

        Assert.Equal(name1, name2);
        Assert.True(name1.Length <= ConfiguredInstanceNameMaxLength);
        Assert.StartsWith(instanceName[..8], name1);
        Assert.NotEqual(instanceName, name1);
    }

    [Theory]
    [InlineData("pod 0")]
    [InlineData("pöd-0")]
    [InlineData("-pod-0")]
    [InlineData("pod-0-")]
    public void ExplicitInstanceName_InvalidCharacters_Throws(string instanceName)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => CreateProvider(new() { InstanceName = instanceName }));

        Assert.Contains("contains invalid characters", exception.Message);
    }

    [Fact]
    public void EnvironmentVariableValue_TooLong_IsShortenedDeterministically()
    {
        var variable = NewVariableName();
        var instanceName = "nexxbiz-executor-api-v3-1-extra-long-replica-0001";
        Environment.SetEnvironmentVariable(variable, instanceName);

        try
        {
            var name1 = CreateProvider(new() { InstanceNameEnvironmentVariable = variable }).GetName();
            var name2 = CreateProvider(new() { InstanceNameEnvironmentVariable = variable }).GetName();

            Assert.Equal(name1, name2);
            Assert.True(name1.Length <= ConfiguredInstanceNameMaxLength);
            Assert.NotEqual(instanceName, name1);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, null);
        }
    }

    [Fact]
    public void ShellClusteringFeature_UsesConfiguredInstanceNameProvider()
    {
        var services = new ServiceCollection();
        var feature = new ShellClusteringFeature
        {
            ApplicationInstanceOptions = options => options.InstanceName = "pod-0"
        };

        services.AddLogging();
        feature.ConfigureServices(services);

        using var serviceProvider = services.BuildServiceProvider();

        var provider = serviceProvider.GetRequiredService<IApplicationInstanceNameProvider>();

        Assert.IsType<ConfiguredApplicationInstanceNameProvider>(provider);
        Assert.Equal("pod-0", provider.GetName());
    }

    [Fact]
    public void ClusteringFeature_UsesConfiguredInstanceNameProvider()
    {
        var services = new ServiceCollection();
        var module = Substitute.For<IModule>();
        module.Services.Returns(services);

        var feature = new ClusteringFeature(module)
        {
            ApplicationInstanceOptions = options => options.InstanceName = "pod-0"
        };

        services.AddLogging();
        feature.Apply();

        using var serviceProvider = services.BuildServiceProvider();

        var provider = serviceProvider.GetRequiredService<IApplicationInstanceNameProvider>();

        Assert.IsType<ConfiguredApplicationInstanceNameProvider>(provider);
        Assert.Equal("pod-0", provider.GetName());
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
