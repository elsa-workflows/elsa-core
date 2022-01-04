using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scripting.JavaScript;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJavaScriptExpressions(this IServiceCollection services)
    {
        return services.AddExpressionHandler<JavaScriptExpressionHandler, JavaScriptExpression>();
    }
}