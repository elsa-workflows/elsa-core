using System.Threading.Tasks;
using Blazored.Modal;
using Blazored.Modal.Services;
using ElsaDashboard.Application.Shared;

namespace ElsaDashboard.Application.Services
{
    public class ConfirmDialogService : IConfirmDialogService
    {
        private readonly IModalService _modalService;

        public ConfirmDialogService(IModalService modalService)
        {
            _modalService = modalService;
        }
        
        public async Task<ModalResult> Show(string title, string message, string confirmButtonText = "Yes")
        {
            var options = new ModalOptions { UseCustomLayout = true };
            var parameters = new ModalParameters();
            parameters.Add("Message", message);
            parameters.Add("ConfirmButtonText", confirmButtonText);
            return await _modalService.Show<ConfirmDialog>(title, parameters, options).Result;
        }
    }
}