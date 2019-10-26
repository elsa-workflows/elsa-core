using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Fluid;

namespace Elsa.Scripting.Liquid
{
    public static class LiquidTemplateManagerExtensions
    {
        /// <summary>
        /// Renders a Liquid template containing HTML.
        /// </summary>
        public static Task<string> RenderAsync(this ILiquidTemplateManager manager, string template, TemplateContext context)
        {
            return manager.RenderAsync(template, context, HtmlEncoder.Default);
        }
    }
}