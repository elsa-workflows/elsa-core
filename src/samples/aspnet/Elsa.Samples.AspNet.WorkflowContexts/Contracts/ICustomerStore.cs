using Elsa.Samples.AspNet.WorkflowContexts.Entities;

namespace Elsa.Samples.AspNet.WorkflowContexts.Contracts;

/// <summary>
/// A sample repository of customers.
/// </summary>
public interface ICustomerStore
{
    Task<Customer?> GetAsync(string id, CancellationToken cancellationToken = default);
    Task SaveAsync(Customer customer, CancellationToken cancellationToken = default);
}