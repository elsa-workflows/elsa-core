# Sample Dashboard Server

This sample project acts as the Blazor dashboard server.
The dashboard web assembly project uses gRPC to communicate with this Blazor server, which then uses Elsa's REST API to manage workflows.

## Blazor Server & Web Assembly

This project can run either in Server Side Blazor mode or Web Assembly Blazor mode. You can control which mode to use by changing the `Program.RuntimeModel` value.

## Elsa Server

This dashboard application depends on a running instance of Elsa (`Elsa.Samples.Server.Host`).