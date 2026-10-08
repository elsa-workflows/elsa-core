# Elsa Studio External Authentication: Blazor Server

This package registers Studio Server as a confidential Elsa External Authentication client. Authorization codes are exchanged on the server, Elsa access and refresh credentials remain in a server-side ticket store, and the browser receives only a secure HTTP-only cookie.

```csharp
builder.Services.AddExternalAuthenticationBroker(options =>
    configuration.GetSection("Authentication:ExternalAuthentication").Bind(options));
```

```json
{
  "Authentication": {
    "Provider": "ExternalAuthentication",
    "ExternalAuthentication": {
      "ClientId": "elsa-studio-server",
      "ClientSecret": "{deployment-secret}",
      "CallbackPath": "/authentication/external/callback",
      "LogoutCallbackPath": "/authentication/external/logout-callback"
    }
  }
}
```

`ClientId` and `ClientSecret` are required. The callback paths are fixed to
`/authentication/external/callback` and `/authentication/external/logout-callback`; register
their absolute Studio URLs with the matching Elsa Authentication Client. Custom callback paths are
rejected at startup. Do not put the client secret in source-controlled configuration.

The cookie is secure, HTTP-only, SameSite Lax, non-sliding, and has an eight-hour lifetime. Refresh credentials are never exposed to Blazor UI code or the browser.
