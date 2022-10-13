using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Models;

namespace Elsa.Activities.Telnyx.Services;

public interface IDestinationPhoneNumberProvider
{
    string Name { get; }
    string DisplayName { get; }
    ValueTask<IEnumerable<string>> GetDestinationPhoneNumbersAsync(DestinationPhoneNumberProviderContext context);
}