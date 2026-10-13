using Microsoft.Extensions.Configuration;

namespace Elsa.Studio.BlazorServer.Helpers
{
    public static class OptionsHelper
    {
        public static TOptions GetOptions<TOptions>(this IConfiguration configuration, string sectionKey)
            where TOptions: new()
        {
            var result = new TOptions();
            configuration.GetSection(sectionKey).Bind(result);
            return result;
        }


        public static TOptions? GetOptionsOrDefault<TOptions>(this IConfiguration configuration, string sectionKey)
            where TOptions : new()
        {
            var section = configuration.GetSection(sectionKey);
            if(!section.Exists())
                return default;

            var result = new TOptions();
            section.Bind(result);
            return result;
        }
    }
}
