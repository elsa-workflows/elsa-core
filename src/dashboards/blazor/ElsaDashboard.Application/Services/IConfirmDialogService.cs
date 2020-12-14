using System.Threading.Tasks;
using Blazored.Modal.Services;

namespace ElsaDashboard.Application.Services
{
    public interface IConfirmDialogService
    {
        Task<ModalResult> Show(string title, string message, string confirmButtonText = "Yes");
    }
}