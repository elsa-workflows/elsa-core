using System;

namespace Elsa.Activities.Http.Services
{
    public interface IAbsoluteUrlProvider
    {
        Uri ToAbsoluteUrl(string relativePath);
    }
}