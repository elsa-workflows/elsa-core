using System;

namespace Elsa.Activities.Http.Contracts
{
    public interface IAbsoluteUrlProvider
    {
        Uri ToAbsoluteUrl(string relativePath);
    }
}