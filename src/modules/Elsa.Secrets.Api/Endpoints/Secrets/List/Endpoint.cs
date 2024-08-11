using Elsa.Abstractions;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Models;
using Elsa.Secrets.Management;
using JetBrains.Annotations;

namespace Elsa.Secrets.Api.Endpoints.Secrets.List;

[UsedImplicitly]
internal class Endpoint(ISecretStore store) : ElsaEndpoint<Request, PagedListResponse<Secret>>
{
    public override void Configure()
    {
        Get("/secrets");
        ConfigurePermissions("read:secrets");
    }

    public override async Task<PagedListResponse<Secret>> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var pageArgs = PageArgs.FromPage(request.Page, request.PageSize);
        var filter = CreateFilter(request);
        var secrets = await FindAsync(filter, pageArgs, cancellationToken);
        return new PagedListResponse<Secret>(secrets);
    }
    
    private SecretFilter CreateFilter(Request request)
    {
        return new SecretFilter
        {
            SearchTerm = request.SearchTerm?.Trim()
        };
    }

    private async Task<Page<Secret>> FindAsync(SecretFilter filter, PageArgs pageArgs, CancellationToken cancellationToken)
    {
        var direction = OrderDirection.Ascending;
        var order = new SecretOrder<DateTimeOffset>
        {
            KeySelector = p => p.CreatedAt,
            Direction = direction
        };

        return await store.FindManyAsync(filter, order, pageArgs, cancellationToken);
    }
}