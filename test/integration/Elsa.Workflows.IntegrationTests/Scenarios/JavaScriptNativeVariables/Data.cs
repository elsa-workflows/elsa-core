using System.Dynamic;

namespace Elsa.Workflows.IntegrationTests.Scenarios.JavaScriptNativeVariables;

public class Data
{
    public static ExpandoObject CreateCustomerModel()
    {
        dynamic customer = new ExpandoObject();
        customer.Name = "John Doe";
        customer.Age = 42;
        customer.Orders = new[]
        {
            new
            {
                Id = 1,
                Product = "Apple",
                Price = 10
            },
            new 
            {
                Id = 3,
                Product = "Orange",
                Price = 30
            },
            new
            {
                Id = 2,
                Product = "Banana",
                Price = 20
            }
        };
        return customer;
    }
}