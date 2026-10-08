# MongoDB variable serializer construction

The consolidated MongoDB provider constructs `VariableSerializer` with the application's dependency-injection-registered `ISerializationTypeRegistry`. This preserves custom storage drivers and variable types when persisted variables are restored.

Code that directly calls the former public parameterless constructor must supply that registry when upgrading:

```csharp
using Elsa.Common.Serialization;
using Elsa.Persistence.MongoDb.Serializers;
using Microsoft.Extensions.DependencyInjection;

var serializer = new VariableSerializer(
    serviceProvider.GetRequiredService<ISerializationTypeRegistry>());
```

The constructor also accepts an optional `ILogger<VariableMapper>`. Applications using the provider's normal MongoDB registration require no manual serializer construction change. Avoid creating a separate default registry: it does not contain the custom types registered by the application.
