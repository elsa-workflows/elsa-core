using System.IO;
using System.Threading.Tasks;
using Elsa.Scripting.Liquid.Options;
using Elsa.Scripting.Liquid.Services;
using Fluid;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Xunit;

namespace Elsa.Scripting.Liquid
{
    public class LiquidParserTests
    {
        [Fact(DisplayName = "Liquid should render subtemplates")]
        public async Task Render()
        {
            var options = new LiquidOptions();

            var parser = new LiquidParser(new OptionsWrapper<LiquidOptions>(options));
            var template = parser.Parse("{% render 'header' %}");

            var root = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), "Scripting", "Liquid");

            var context = new TemplateContext(options)
            {
                Options =
                {
                    FileProvider = new PhysicalFileProvider(root)
                }
            };
            var rendered = await template.RenderAsync(context);

            Assert.Equal("<h2>YES</h2>", rendered);
        }
    }
}
