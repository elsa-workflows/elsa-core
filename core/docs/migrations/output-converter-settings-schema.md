# Output converter settings schema is no longer evaluated server-side

> **Behavior change in a patch release.** 3.10.1 changes behavior compared with 3.10.0. It removes server-side `SettingsSchema` evaluation and drops JsonSchema.Net, JsonPointer.Net and Json.More.Net from the `Elsa.Workflows.Core` package graph. It ships in a patch so 3.10.x users can remove those dependencies without waiting for 3.11 (see #9140). Before upgrading, check custom converters that declare a `SettingsSchema`, and any code that uses those package types without its own package reference.

From 3.10.1, the server no longer evaluates an output converter's `SettingsSchema`. It has done so since 3.8.0.

Core still rejects settings that are not a JSON object. Beyond that shape check, `IOutputConverter.ValidateSettings` is the only server-side settings check, both when definitions are validated and at runtime. Custom converters that relied on the schema must move their required, type, range, enum and pattern checks into `ValidateSettings`. Without that, invalid settings are accepted at both stages.

`SettingsSchema` remains on the descriptor as metadata for Studio and other tools.

Code that used JsonSchema.Net types through the `Elsa.Workflows.Core` package must now add its own direct package reference.
