using Microsoft.AspNetCore.Http;

namespace Elsa.Http.Options
{
    public class HttpActivityOptions
    {
        /// <summary>
        /// The root path at which HTTP activities can be invoked.
        /// </summary>
        public PathString? BasePath { get; set; }
    }
}