using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.UI.Models;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <inheritdoc />
public class DefaultActivityDisplaySettingsRegistry : IActivityDisplaySettingsRegistry
{
    private readonly IEnumerable<IActivityDisplaySettingsProvider> _providers;
    private IDictionary<string, ActivityDisplaySettings>? _settings;
    private const string defaultIcon = @"
        <svg style=""width:24px;height:24px"" viewBox=""0 0 24 24"">
            <path fill=""currentColor"" d=""M21,16.5C21,16.88 20.79,17.21 20.47,17.38L12.57,21.82C12.41,21.94 12.21,22 12,22C11.79,22 11.59,21.94 11.43,21.82L3.53,17.38C3.21,17.21 3,16.88 3,16.5V7.5C3,7.12 3.21,6.79 3.53,6.62L11.43,2.18C11.59,2.06 11.79,2 12,2C12.21,2 12.41,2.06 12.57,2.18L20.47,6.62C20.79,6.79 21,7.12 21,7.5V16.5M12,4.15L6.04,7.5L12,10.85L17.96,7.5L12,4.15M5,15.91L11,19.29V12.58L5,9.21V15.91M19,15.91V9.21L13,12.58V19.29L19,15.91Z"" />
        </svg>";

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultActivityDisplaySettingsRegistry"/> class.
    /// </summary>
    public DefaultActivityDisplaySettingsRegistry(IEnumerable<IActivityDisplaySettingsProvider> providers)
    {
        _providers = providers;
    }

    private static ActivityDisplaySettings DefaultSettings { get; set; } = new(DefaultActivityColors.Default, defaultIcon);

    /// <inheritdoc />
    public ActivityDisplaySettings GetSettings(string activityType)
    {
        var dictionary = GetSettingsDictionary();
        return dictionary.TryGetValue(activityType, out var settings) ? settings : DefaultSettings;
    }

    /// <inheritdoc />
    public void MarkStale()
    {
        _settings = null;
    }

    private IDictionary<string, ActivityDisplaySettings> GetSettingsDictionary()
    {
        if(_settings != null)
            return _settings;
        
        var settings = new Dictionary<string, ActivityDisplaySettings>();

        foreach (var provider in _providers)
        {
            var providerSettings = provider.GetSettings();
            
            foreach (var (activityType, activityDisplaySettings) in providerSettings)
                settings[activityType] = activityDisplaySettings;
        }
        
        _settings = settings;
        return _settings;
    }
}