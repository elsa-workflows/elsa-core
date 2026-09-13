using System.Text.Json.Nodes;
using Elsa.AI.Host.Options;
using Elsa.AI.Host.Services;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;
using System.Threading.Tasks;

namespace Elsa.AI.Host.UnitTests.Grounding;

public class AIGroundingResultFormatterTests
{
    private readonly AIGroundingResultFormatter _formatter = new(MicrosoftOptions.Create(new AIHostOptions
    {
        Grounding = new AIGroundingOptions
        {
            MaxItems = 1,
            MaxResultBytes = 16 * 1024
        }
    }));

    [Test]
    [DisplayName("Formatter redacts sensitive keys before returning tool data")]
    public async Task FormatterRedactsSensitiveKeys()
    {
        var result = _formatter.CreateResult(
            "done",
            [
                new JsonObject
                {
                    ["name"] = "HTTP",
                    ["apiKey"] = "secret-value",
                    ["nested"] = new JsonObject { ["password"] = "also-secret" }
                }
            ],
            1);

        var item = result.Data["items"]!.AsArray()[0]!.AsObject();

        await Assert.That(item["name"]!.GetValue<string>()).IsEqualTo("HTTP");
        await Assert.That(item["apiKey"]!.GetValue<string>()).IsEqualTo("***");
        await Assert.That(item["nested"]!.AsObject()["password"]!.GetValue<string>()).IsEqualTo("***");
    }

    [Test]
    [DisplayName("Formatter clamps result item count")]
    public async Task FormatterClampsResultItems()
    {
        var result = _formatter.CreateResult("done", [new JsonObject { ["id"] = "1" }, new JsonObject { ["id"] = "2" }], 2);

        await Assert.That(result.Data["truncated"]!.GetValue<bool>()).IsTrue();
        await Assert.That(result.Data["returned"]!.GetValue<int>()).IsEqualTo(1);
        await Assert.That(result.Data["items"]!.AsArray()).HasSingleItem();
    }
}