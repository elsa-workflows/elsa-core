# Output converter settings schema is no longer evaluated server-side

From 3.11, the server no longer evaluates an output converter's `SettingsSchema`. It has done so since 3.8.0.

Core still rejects settings that are not a JSON object. Beyond that shape check, `IOutputConverter.ValidateSettings` is the only server-side settings check, both when definitions are validated and at runtime. Custom converters that relied on the schema must move their required, type, range, enum and pattern checks into `ValidateSettings`. Without that, invalid settings are accepted at both stages.

`SettingsSchema` remains on the descriptor as metadata for Studio and other tools.

Code that used JsonSchema.Net types through the `Elsa.Workflows.Core` package must now add its own direct package reference.
