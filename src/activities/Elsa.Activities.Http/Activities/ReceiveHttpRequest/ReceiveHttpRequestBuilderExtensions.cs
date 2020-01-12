using System;
using System.Net.Http;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Http;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;
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