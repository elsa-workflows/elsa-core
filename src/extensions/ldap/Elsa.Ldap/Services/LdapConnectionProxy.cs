using System.DirectoryServices.Protocols;
using Elsa.Ldap.Contracts;
using Elsa.Ldap.Extensions;

namespace Elsa.Ldap.Services;

internal class LdapConnectionProxy : ILdapConnection
{
    private readonly LdapConnection _connection;
    private bool _disposed;

    public LdapConnectionProxy(LdapConnection connection)
    {
        _connection = connection;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _connection.Dispose();
            }
            
            _disposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public async Task<AddResponse> SendRequestAsync(AddRequest request, CancellationToken cancellationToken = default)
    {
        return await _connection.SendRequestAsync<AddResponse>(request, cancellationToken);
    }

    public async Task<DeleteResponse> SendRequestAsync(DeleteRequest request, CancellationToken cancellationToken = default)
    {
        return await _connection.SendRequestAsync<DeleteResponse>(request, cancellationToken);
    }

    public async Task<ModifyResponse> SendRequestAsync(ModifyRequest request, CancellationToken cancellationToken = default)
    {
        return await _connection.SendRequestAsync<ModifyResponse>(request, cancellationToken);
    }

    public async Task<ModifyDNResponse> SendRequestAsync(ModifyDNRequest request, CancellationToken cancellationToken = default)
    {
        return await _connection.SendRequestAsync<ModifyDNResponse>(request, cancellationToken);
    }

    public async Task<CompareResponse> SendRequestAsync(CompareRequest request, CancellationToken cancellationToken = default)
    {
        return await _connection.SendRequestAsync<CompareResponse>(request, cancellationToken);
    }

    public async Task<SearchResponse> SendRequestAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        return await _connection.SendRequestAsync<SearchResponse>(request, cancellationToken);
    }

    public async Task<ExtendedResponse> SendRequestAsync(ExtendedRequest request, CancellationToken cancellationToken = default)
    {
        return await _connection.SendRequestAsync<ExtendedResponse>(request, cancellationToken);
    }
}
