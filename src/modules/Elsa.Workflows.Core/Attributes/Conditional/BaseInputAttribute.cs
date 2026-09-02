using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Elsa.Workflows.Attributes.Conditional;
[AttributeUsage(AttributeTargets.Property)]
public class Input : InputAttribute
{
    public Input(List<string>? descriptions = null)
    {
        if (descriptions is not null)
        {
            UIDescription = descriptions;
        }
    }

    protected void Serialize(object? extendWithValues = null)
    {
        // Important:
        // The description field is used to send the payload to the server.
        Description = JsonConvert.SerializeObject(new
        {
            InputType = InputType,
            Description = UIDescription
        });

        if (extendWithValues is not null)
        {
            var json = (JsonConvert.DeserializeAnonymousType(Description, extendWithValues) as JObject)!;
            Description = json.ToString();
        }
    }

    public string InputType { get; set; } = "Generic";
    public List<string> UIDescription { get; set; } = ["Generic Input"];
}
