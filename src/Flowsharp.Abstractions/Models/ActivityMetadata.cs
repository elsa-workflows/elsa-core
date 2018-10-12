using Microsoft.Extensions.Localization;

namespace Flowsharp.Abstractions.Models
{
    public class ActivityMetadata
    {
        public LocalizedString DisplayName { get; set; }
        public LocalizedString Category { get; set; }
    }
}
