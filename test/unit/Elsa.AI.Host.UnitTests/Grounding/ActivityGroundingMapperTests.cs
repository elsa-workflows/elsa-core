using Elsa.AI.Host.Services;
using Elsa.Workflows.Models;
using System.Threading.Tasks;

namespace Elsa.AI.Host.UnitTests.Grounding;

public class ActivityGroundingMapperTests
{
    [Test]
    [DisplayName("Activity mapper emits model-safe descriptor metadata")]
    public async Task ActivityMapperEmitsModelSafeDescriptorMetadata()
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

        await Assert.That(summary.Type).IsEqualTo("Elsa.Http.HttpEndpoint");
        await Assert.That(summary.IsTrigger).IsTrue();
        var input = await Assert.That(summary.Inputs).HasSingleItem();
        await Assert.That(input.IsSensitive).IsTrue();
        await Assert.That(input.Type).IsEqualTo("String");
        await Assert.That(summary.Ports).Contains("Done");
    }
}