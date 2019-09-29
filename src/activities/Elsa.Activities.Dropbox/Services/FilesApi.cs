using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Dropbox.Extensions;
using Elsa.Activities.Dropbox.Models;
using Newtonsoft.Json;

namespace Elsa.Activities.Dropbox.Services
{
    public class FilesApi : IFilesApi
    {
        private readonly HttpClient httpClient;

        public FilesApi(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }
        
        public async Task<UploadResponse> UploadAsync(UploadRequest request, byte[] file, CancellationToken cancellationToken)
        {
            var content = new ByteArrayContent(file);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Headers.Add("Dropbox-API-Arg", request.ToString());
            
            var response = await httpClient.PostAsync("/2/files/upload", content, cancellationToken);

            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<UploadResponse>(json, new JsonSerializerSettings().ConfigureForDropboxApi());
        }
    }
}