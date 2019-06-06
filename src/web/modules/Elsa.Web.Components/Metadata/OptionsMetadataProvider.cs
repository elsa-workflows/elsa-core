using System.Linq;
using Elsa.Web.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Elsa.Web.Components.Metadata
{
    public class OptionsMetadataProvider : IDisplayMetadataProvider
    {
        public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
        {
            var optionsAttribute = context.PropertyAttributes?.OfType<OptionsAttribute>().FirstOrDefault();

            if (optionsAttribute == null)
                return;

            if (optionsAttribute.OptionLabel != null)
                context.DisplayMetadata.Placeholder = () => optionsAttribute.OptionLabel;

            context.DisplayMetadata.AdditionalValues["Options"] = optionsAttribute.Options;
        }
    }
}