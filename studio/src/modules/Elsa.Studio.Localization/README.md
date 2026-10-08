# Elsa Studio Localization
Elsa Studio Localization Allows localizing the Studio UI to different languages.

`ILocalizationProvider` is the interface that needs to be implemented to provide the localized strings for the Studio UI.

Create your own localization file (Using the resource file or any alternative method' and implement the `ILocalizationProvider` interface.

Afterwards, register your implementation in the DI container.

```cssharp
builder.Services.AddSingleton<ILocalizationProvider, MyLocalizationProvider>();
```

## Steps to Enable Localization

### For Blazor Server
1. Add Reference to `Elsa.Studio.Localization.BlazorServer` package.
1. Define the Localization Configuration
    ```csharp
    var localizationConfig = new LocalizationConfig
	{
	    ConfigureLocalizationOptions = options =>
	    {
	        configuration.GetSection(LocalizationOptions.LocalizationSection).Bind(options);
	        options.SupportedCultures = new[] { options?.DefaultCulture ?? new LocalizationOptions().DefaultCulture }
	            .Concat(options?.SupportedCultures.Where(culture => culture != options?.DefaultCulture) ?? []) .ToArray();
	    }
	};
    ```
1. Register the Localization Module `builder.Services.AddLocalizationModule(localizationConfig);`
1. Register your locallization provider
	```csharp
	builder.Services.AddSingleton<ILocalizationProvider, MyLocalizationProvider>();
	```
1. Add The localization Middleware. Making Sure that Controllers are also mapped.
    ```csharp
    app.UseElsaLocalization();
    app.MapControllers();
    ```
1. Add below configuration in the `appsettings.json` file, specifying the supported cultures.
    ```json
    "Localization": {
        "SupportedCultures": [
          "ja-JP",
          "fr-FR"
        ]
      }
    ```

### For Blazor WebAssembly
1. Add Reference to `Elsa.Studio.Localization.BlazorWasm` package.
1. Define the Localization Configuration
    ```csharp
    var localizationConfig = new LocalizationConfig
	{
	    ConfigureLocalizationOptions = options =>
	    {
	        configuration.GetSection(LocalizationOptions.LocalizationSection).Bind(options);
	        options.SupportedCultures = new[] { options?.DefaultCulture ?? new LocalizationOptions().DefaultCulture }
	            .Concat(options?.SupportedCultures.Where(culture => culture != options?.DefaultCulture) ?? []) .ToArray();
	    }
	};
    ```
1. Register the Localization Module `builder.Services.AddLocalizationModule(localizationConfig);`
1. Register your locallization provider
	```csharp
	builder.Services.AddSingleton<ILocalizationProvider, MyLocalizationProvider>();
	```
1. Use the localization Middleware
    ```csharp
    await app.UseElsaLocalization();
    ```
1. Add below configuration in the `appsettings.json` file, specifying the supported cultures.
    ```json
    "Localization": {
        "SupportedCultures": [
          "ja-JP",
          "fr-FR"
        ]
      }
    ```
