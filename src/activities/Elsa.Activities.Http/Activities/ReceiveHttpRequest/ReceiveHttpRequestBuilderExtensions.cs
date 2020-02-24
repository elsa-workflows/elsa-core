using Elsa.Builders;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    public static class ReceiveHttpRequestBuilderExtensions
    {
        public static ActivityBuilder ReceiveHttpRequest(
            this IBuilder builder,
            PathString path,
            string method = default,
            bool? readContent = default) => builder.Then<ReceiveHttpRequest>(x => x
            .WithPath(path)
            .WithMethod(method ?? HttpMethods.Get)
            .WithReadContent(readContent ?? false));
    }
}