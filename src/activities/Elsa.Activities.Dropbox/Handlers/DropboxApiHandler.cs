using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.Dropbox.Handlers
{
    public class DropboxApiHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            //request.Content = new ByteArrayContent(request.Content.);
            var response = await base.SendAsync(request, cancellationToken);
            var error = await response.Content.ReadAsStringAsync();
            
            return response;
        }
    }
}