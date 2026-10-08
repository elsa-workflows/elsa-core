using System.Text.Json.Nodes;
using Elsa.AI.Host.Options;
using Elsa.AI.Host.Services;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;

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

    [Fact(DisplayName = "Formatter redacts sensitive keys before returning tool data")]
    public void FormatterRedactsSensitiveKeys()
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

        Assert.Equal("HTTP", item["name"]!.GetValue<string>());
        Assert.Equal("***", item["apiKey"]!.GetValue<string>());
        Assert.Equal("***", item["nested"]!.AsObject()["password"]!.GetValue<string>());
    }

    [Fact(DisplayName = "Formatter clamps result item count")]
    public void FormatterClampsResultItems()
    {
        var result = _formatter.CreateResult("done", [new JsonObject { ["id"] = "1" }, new JsonObject { ["id"] = "2" }], 2);

        Assert.True(result.Data["truncated"]!.GetValue<bool>());
        Assert.Equal(1, result.Data["returned"]!.GetValue<int>());
        Assert.Single(result.Data["items"]!.AsArray());
    }
}
