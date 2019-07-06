using System.Threading.Tasks;

namespace Elsa.Activities.Http.Services
{
    public interface IRequestHandler
    {
        Task<IRequestHandlerResult> HandleRequestAsync();
    }
}