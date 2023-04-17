using Elsa.Samples.WorkflowContexts.Contracts;

namespace Elsa.Samples.WorkflowContexts.Services;

/// <summary>
/// An in-memory implementation of <see cref="ICustomerStore"/>.
/// </summary>
public class MemoryCustomerStore : ICustomerStore
{
    private readonly IDictionary<string, Customer> _customers = CreateCustomersDatabase();

    public Task<Customer?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_customers.TryGetValue(id, out var customer) ? customer : null);
    }

    public Task SaveAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        _customers[customer.Id] = customer;
        return Task.CompletedTask;
    }

    private static IDictionary<string, Customer> CreateCustomersDatabase()
    {
        return new Dictionary<string, Customer>
        {
            ["1"] = new()
            {
                Id = "1",
                Name = "John Doe",
                Email = "john.doe@acme.com",
                Phone = "+1 555 123 4567",
                Website = "https://acme.com"
            },
            ["2"] = new()
            {
                Id = "2",
                Name = "Alice Smith",
                Email = "alice.smith@example.com",
                Phone = "+1 555 123 4567",
                Website = "https://example.com"
            },
            ["3"] = new()
            {
                Id = "3",
                Name = "Bob Jones",
                Email = "bob.jones@supplier.com",
                Phone = "+1 555 123 4567",
                Website = "https://supplier.com"
            },
        };
    }
}