using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Adapters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Elsa.Dashboard.Microsoft
{
    public static class JsonPatchDocumentExtensions
    {
        public static void CustomApplyTo<T>(this JsonPatchDocument<T> patch, T value, ModelStateDictionary modelState) where T : class
        {
            patch.ApplyTo(
                value,
                new ObjectAdapter(
                    patch.ContractResolver,
                    (jsonPatchError =>
                    {
                        var name = jsonPatchError.AffectedObject.GetType().Name;
                        modelState.TryAddModelError(name, jsonPatchError.ErrorMessage);
                    }),
                    new CustomAdapterFactory()
                )
            );
        }
    }
}