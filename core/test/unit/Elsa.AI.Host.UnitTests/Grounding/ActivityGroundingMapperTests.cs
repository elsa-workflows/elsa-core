using Elsa.AI.Host.Services;
using Elsa.Workflows.Models;

namespace Elsa.AI.Host.UnitTests.Grounding;

public class ActivityGroundingMapperTests
{
    [Fact(DisplayName = "Activity mapper emits model-safe descriptor metadata")]
    public void ActivityMapperEmitsModelSafeDescriptorMetadata()
    {
        var mapper = new ActivityGroundingMapper();
        var descriptor = new ActivityDescriptor
        {
            TypeName = "Elsa.Http.HttpEndpoint",
            Namespace = "Elsa.Http",
            Name = "HttpEndpoint",
            DisplayName = "HTTP Endpoint",
            Description = "Receives HTTP requests",
            Category = "HTTP",
            Version = 2,
            IsStart = true,
            IsBrowsable = true,
            Inputs =
            {
                new InputDescriptor
                {
                    Name = "ApiKey",
                    DisplayName = "API key",
                    Description = "A sensitive input",
                    Type = typeof(string),
                    UIHint = "single-line",
                    IsSensitive = true
                }
            },
            Outputs =
            {
                new OutputDescriptor { Name = "Body", DisplayName = "Body", Type = typeof(string) }
            },
            Ports = { new Port { Name = "Done" } }
        };

        var summary = mapper.Map(descriptor);

        Assert.Equal("Elsa.Http.HttpEndpoint", summary.Type);
        Assert.True(summary.IsTrigger);
        var input = Assert.Single(summary.Inputs);
        Assert.True(input.IsSensitive);
        Assert.Equal("String", input.Type);
        Assert.Contains("Done", summary.Ports);
    }
}
