using System;
using Elsa.Activities.Http;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    public static class ReceiveHttpRequestExtensions
    {
        public static ReceiveHttpRequest WithPath(this ReceiveHttpRequest activity, PathString value) => activity.With(x => x.Path, value);
        public static ReceiveHttpRequest WithMethod(this ReceiveHttpRequest activity, string value) => activity.With(x => x.Method, value);
        public static ReceiveHttpRequest WithReadContent(this ReceiveHttpRequest activity, bool value) => activity.With(x => x.ReadContent, value);
    }
}