using System.DirectoryServices.Protocols;

namespace Elsa.Ldap.Contracts;

internal interface ILdapConnection : IDisposable
{
    public Task<AddResponse> SendRequestAsync(AddRequest request, CancellationToken cancellationToken = default);
    public Task<DeleteResponse> SendRequestAsync(DeleteRequest request, CancellationToken cancellationToken = default);
    public Task<ModifyResponse> SendRequestAsync(ModifyRequest request, CancellationToken cancellationToken = default);
    public Task<ModifyDNResponse> SendRequestAsync(ModifyDNRequest request, CancellationToken cancellationToken = default);
    public Task<CompareResponse> SendRequestAsync(CompareRequest request, CancellationToken cancellationToken = default);
    public Task<SearchResponse> SendRequestAsync(SearchRequest request, CancellationToken cancellationToken = default);
    public Task<ExtendedResponse> SendRequestAsync(ExtendedRequest request, CancellationToken cancellationToken = default);
}
