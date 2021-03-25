using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Client.Models;
using Refit;

namespace Elsa.Activities.Telnyx.Extensions
{
    public static class ApiExceptionExtensions
    {
        public static async Task<ErrorResponse> GetErrorResponseAsync(this ApiException e, CancellationToken cancellationToken = default)
        {
            var httpContent = new StringContent(e.Content!);
            return (await e.RefitSettings.ContentSerializer.FromHttpContentAsync<ErrorResponse>(httpContent, cancellationToken))!;
        }

        public static async Task<bool> CallIsNoLongerActiveAsync(this ApiException e, CancellationToken cancellationToken = default)
        {
            var errorResponse = await e.GetErrorResponseAsync(cancellationToken);
            var errors = errorResponse.Errors;
            return errors.Any(x => x.Code == ErrorCodes.CallHasAlreadyEnded);
        }
    }
}