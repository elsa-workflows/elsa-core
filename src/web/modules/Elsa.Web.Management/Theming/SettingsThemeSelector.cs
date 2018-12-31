using System.Threading.Tasks;
using OrchardCore.DisplayManagement.Theming;
using OrchardCore.Environment.Shell;

namespace Elsa.Web.Management.Theming
{
    public class SettingsThemeSelector : IThemeSelector
    {
        private readonly ShellSettings shellSettings;

        public SettingsThemeSelector(ShellSettings shellSettings)
        {
            this.shellSettings = shellSettings;
        }
        
        public Task<ThemeSelectorResult> GetThemeAsync()
        {
            var currentThemeName = shellSettings.Configuration["CurrentTheme"];
            
            var result = new ThemeSelectorResult
            {
                Priority = 0,
                ThemeName = currentThemeName
            };

            return Task.FromResult(result);
        }
    }
}