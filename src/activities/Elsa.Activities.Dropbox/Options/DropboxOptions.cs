using System;

namespace Elsa.Activities.Dropbox.Options
{
    public class DropboxOptions
    {
        public string AccessToken { get; set; } = default!;
        public Uri? ContentServiceUrl { get; set; } = default!;
    }
}