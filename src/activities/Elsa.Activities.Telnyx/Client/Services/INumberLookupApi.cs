using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Client.Models;
using Refit;

namespace Elsa.Activities.Telnyx.Client.Services;

public interface INumberLookupApi
{
    [Get("/v2/number_lookup/{phoneNumber}")]
    Task<TelnyxResponse<NumberLookupResponse>> NumberLookupAsync(
        string phoneNumber,
        [Query(CollectionFormat.Multi)] [AliasAs("type")]
        IEnumerable<string>? types = default,
        CancellationToken cancellationToken = default);
}