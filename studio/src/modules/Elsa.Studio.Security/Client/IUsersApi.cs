using Elsa.Studio.Security.Models;
using Refit;

namespace Elsa.Studio.Security.Client;

public interface IUsersApi
{
    [Get("/identity/users")]
    Task<ListUsersResponse> ListAsync(CancellationToken cancellationToken = default);

    [Post("/identity/users")]
    Task<CreateUserResponse> CreateAsync([Body] CreateUserRequest request, CancellationToken cancellationToken = default);

    [Put("/identity/users/{id}")]
    Task<UserSummary> UpdateAsync(string id, [Body] UpdateUserRequest request, CancellationToken cancellationToken = default);

    [Delete("/identity/users/{id}")]
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
