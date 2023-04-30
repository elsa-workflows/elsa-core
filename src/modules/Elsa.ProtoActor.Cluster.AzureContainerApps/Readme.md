# Azure Container Apps cluster provider

Use this cluster provider when you're hosting your application in an Azure Container Apps cluster.

The provider stores cluster member information using Azure Resource Tags on the container application and uses these tags to discover other cluster members within the same Azure Container Application Managed Environment.

## Installation

To install the provider, add the following code to your program:

```csharp
services.AddAzureContainerAppsProvider(ArmClientProviders.DefaultAzureCredential, options =>
{
   options.ResourceGroupName = "{the resource group name containing your container application}";
   
   // Optionally, you can specify the subscription ID. If not specified, the provider will use the default subscription.
   options.SubscriptionId = "{your subscription id}";
});
```

## Appsettings.json

Instead of hardcoding the options above, you should instead bind the options using your app's configuration.
For example, consider the following appsettings.json:

```json
{
  "AzureContainerApps": {
    "ResourceGroupName": "{the resource group name containing your container application}",
    "SubscriptionId": "{your subscription id}"
  }
}
```

You can then bind the options using the following code:

```csharp
services.AddAzureContainerAppsProvider(ArmClientProviders.DefaultAzureCredential, options => configuration.Bind("AzureContainerApps", options));
```

## Custom member store

By default, the provider will store cluster member information using Azure Resource Tags on the container application.
If you want to use a custom storage mechanism, you can do so by implementing the `IClusterMemberStore` interface and registering it with the service collection.

For example, consider the following implementation:

```csharp
public class RedisClusterMemberStorage : IClusterMemberStore
{
  ...
}
```

You can then register the implementation with the service collection:

```csharp
services.AddSingleton<IClusterMemberStore, RedisClusterMemberStore>();
```