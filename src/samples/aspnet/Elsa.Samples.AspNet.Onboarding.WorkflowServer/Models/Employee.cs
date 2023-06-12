using System.Text.Json.Serialization;

namespace Elsa.Samples.AspNet.Onboarding.WorkflowServer.Models;

public class Employee
{
    [JsonConstructor]
    public Employee()
    {
        
    }
    
    public Employee(string name, string email)
    {
        Name = name;
        Email = email;
    }

    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
}