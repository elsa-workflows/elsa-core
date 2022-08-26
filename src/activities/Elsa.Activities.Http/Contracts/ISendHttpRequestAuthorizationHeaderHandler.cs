using System.Threading.Tasks;

namespace Elsa.Activities.Http.Contracts; 

public interface ISendHttpRequestAuthorizationHeaderHandler {
    Task<string> EvaluateStoredValue(string originalValue);
}