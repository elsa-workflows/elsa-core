using System.Reflection;
using Elsa.Expressions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Elsa.Web.Components.Metadata
{
    public class WorkflowExpressionDataTypeMetadataProvider : IDisplayMetadataProvider
    {
        public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
        {
            if (typeof(WorkflowExpression).IsAssignableFrom(context.Key.ModelType))
            {
                context.DisplayMetadata.DataTypeName = "WorkflowExpression";
            }
        }
    }
}