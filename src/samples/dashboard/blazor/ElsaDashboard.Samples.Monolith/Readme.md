# Sample Dashboard Monolith

This sample project acts as both the Blazor dashboard as well as the Elsa Workflows server.
The dashboard web assembly project still uses gRPC to communicate with the Blazor server, which then uses Elsa's REST API to manage workflows.

## Blazor Server & Web Assembly

This project can run either in Server Side Blazor mode or Web Assembly Blazor mode. You can control which mode to use by changing the `Program.RuntimeModel` value.