using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Dropbox.Models;

namespace Elsa.Activities.Dropbox.Services
{
    public interface IFilesApi
    {
        Task<UploadResponse> UploadAsync(UploadRequest request, byte[] file, CancellationToken cancellationToken);
    }
}