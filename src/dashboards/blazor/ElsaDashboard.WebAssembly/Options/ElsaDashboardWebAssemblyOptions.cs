using System;

namespace ElsaDashboard.WebAssembly.Options
{
    public class ElsaDashboardWebAssemblyOptions
    {
        /// <summary>
        /// The URL of the dashboard's backend.
        /// When the web assembly project is hosted from an ASP.NET Core host, that host will be used by default.
        /// When the web assembly project on the other hand is hosted directly from e.g. blob storage, the URL to the gRPC backend needs to be specified explicitly.  
        /// </summary>
        public Uri? BackendUrl { get; set; }
    }
}