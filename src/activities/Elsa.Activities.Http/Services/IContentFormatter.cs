using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elsa.Activities.Http.Services
{
    public interface IContentFormatter
    {
        int Priority { get; }
        IEnumerable<string> SupportedContentTypes { get; }
        Task<object> FormatAsync(string content, string contentType);
    }
}