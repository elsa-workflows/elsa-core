# Logging Framework

The logging framework provides a flexible and extensible way to capture, structure, and route log entries to various sinks.
You can configure logging programmatically or via configuration files, and extend the framework with custom sinks for complete control.

## Programmatic Configuration

You can configure logging sinks directly in your application code. For example, in your `Program.cs`:

```csharp
elsa.UseLoggingFramework(logging =>
{
    // Use built-in sinks.
    logging.UseConsole();
    logging.UseSerilog();

    // Configure sinks from appsettings.json.
    logging.ConfigureDefaults(options => configuration.GetSection("LoggingFramework").Bind(options));

    // Add sinks manually.
    logging.AddLogSink(new LoggerSink("Console (via code)", consoleLogger));
    logging.AddLogSink(new LoggerSink("File (pretty)", filePrettyFactory));
    logging.AddLogSink(new LoggerSink("File (JSON)", fileJsonFactory));
});
```

This example demonstrates how to use built-in sinks, bind configuration from `appsettings.json`, and manually add custom sinks.

## Configuration via appsettings.json

You can also configure logging sinks declaratively in your `appsettings.json` file:

```json
{
  "LoggingFramework": {
    "Defaults": [
      "Console",
      "FilePretty",
      "FileJson"
    ],
    "Sinks": [
      {
        "Type": "Console",
        "Name": "Console",
        "Options": {
          "MinLevel": "Information",
          "CategoryFilters": {
            "Process": "Information",
            "Process.Nested": "Debug",
            "Process.Nested.Inner": "Information"
          },
          "Formatter": "Default",
          "TimestampFormat": "HH:mm:ss ",
          "DisableColors": true
        }
      },
      {
        "Type": "Serilog",
        "Name": "FilePretty",
        "Options": {
          "Path": "App_Data/logs/activity-pretty-.log",
          "RollingInterval": "Day",
          "Template": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
          "MinLevel": "Information"
        }
      },
      {
        "Type": "Serilog",
        "Name": "FileJson",
        "Options": {
          "Path": "App_Data/logs/activity-json-.log",
          "RollingInterval": "Day",
          "Formatter": "CompactJson",
          "MinLevel": "Debug"
        }
      }
    ]
  }
}
```

Each sink specifies its type, name, and options. Elsa will automatically discover and configure these sinks at startup.

## Log Activity

Elsa provides a built-in `Log` activity for emitting structured log entries from workflows. The activity supports the following properties:

- **Message**: The log message to emit.
- **Level**: The log level (Trace, Debug, Information, Warning, Error, Critical).
- **Category**: The log category (defaults to "Process").
- **Arguments**: Values for placeholders in the log message.
- **Attributes**: Additional key/value pairs to include as attributes.
- **SinkNames**: Target sinks to write to (can be selected from available sinks).

Example usage in a workflow:

```csharp
new Log("Workflow started", LogLevel.Information)
```

You can also specify sinks and attributes:

```csharp
new Log
{
    Message = new("Order received: {OrderId}"),
    Arguments = new(new { OrderId = orderId }),
    SinkNames = new(new[] { "FileJson" })
}
```

## Extending with Custom Sinks

For complete control over logging, it is recommended to implement your own `ILogSinkFactory`. This allows you to create custom logging sinks tailored to your requirements. Elsa provides examples such as `ConsoleLogSinkFactory` and `SerilogLogSinkFactory` that you can use as references.

To implement a custom sink:

1. Create a class that implements `ILogSinkFactory<TOptions>`.
2. Register your factory in the DI container.
3. Reference your sink type in configuration or code.

Example:

```csharp
public class MyCustomLogSinkFactory : ILogSinkFactory<MyCustomOptions>
{
    public string Type => "MyCustom";
    public ILogSink Create(string name, MyCustomOptions options)
    {
        // Create and return your custom sink.
    }
}

// Register in DI:
services.AddScoped<ILogSinkFactory, MyCustomLogSinkFactory>();
```

## References
- See `ConsoleLogSinkFactory` and `SerilogLogSinkFactory` for implementation examples.
- Configure sinks in code or via configuration for maximum flexibility.
- Use the `Log` activity in workflows to emit structured log entries.
