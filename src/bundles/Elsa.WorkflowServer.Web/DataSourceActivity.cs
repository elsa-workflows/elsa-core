using Bogus;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.WorkflowServer.Web;

[Activity("Demo", "Data Source", "Generates a collection of random strings.")]
public class DataSourceActivity : CodeActivity<ICollection<string>>
{
    [Input(Description = "The number of items to generate.")]
    public Input<int> NumberOfItems { get; set; } = default!;


    protected override void Execute(ActivityExecutionContext context)
    {
        var randomizer = new Randomizer();
        var randomStrings = new List<string>();
        var numberOfItems = context.Get(NumberOfItems);

        for (var i = 0; i < numberOfItems; i++)
        {
            var randomString = randomizer.String2(10);
            randomStrings.Add(randomString);
        }

        Result.Set(context, randomStrings);
    }
}