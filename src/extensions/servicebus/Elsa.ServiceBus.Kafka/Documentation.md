# Elsa.ServiceBus.Kafka

Kafka integration module for [Elsa Workflows](https://v3.elsaworkflows.io/).  
Provides two workflow activities (`MessageReceived`, `ProduceMessage`) and a fully configurable consumer/producer infrastructure with multi-tenancy and Avro schema support.

---

## Table of Contents

- [Quick Start](#quick-start)
1. [Registration](#1-registration)
2. [Configuration Reference](#2-configuration-reference)
3. [Topics](#3-topics)
4. [Schema Registries](#4-schema-registries)
5. [Consumers & Consumer Factories](#5-consumers--consumer-factories)
6. [Producers & Producer Factories](#6-producers--producer-factories)
7. [Activities](#7-activities)
   - [MessageReceived](#71-messagereceived)
   - [ProduceMessage](#72-producemessage)
8. [Correlation Strategies](#8-correlation-strategies)
9. [Multi-Tenancy](#9-multi-tenancy)
10. [Extensibility](#10-extensibility)

---

## Quick Start

This is the minimum setup to trigger a workflow from a Kafka message.

### Step 1 — Register the module

```csharp
services.AddElsa(elsa =>
{
    elsa.UseKafka(kafka =>
    {
        kafka.ConfigureOptions(options =>
        {
            // Declare a consumer that deserializes JSON messages into dynamic objects.
            options.Consumers.Add(new ConsumerDefinition
            {
                Id = "my-consumer",
                Name = "My Consumer",
                FactoryType = typeof(ExpandoObjectConsumerFactory),
                Config = new ConsumerConfig
                {
                    BootstrapServers = "localhost:9092",
                    GroupId = "workflow-engine",
                    AutoOffsetReset = AutoOffsetReset.Earliest
                }
            });
        });
    });
});
```

For SASL/SSL (e.g. Confluent Cloud or MSK) add the relevant properties to `ConsumerConfig`:

```csharp
Config = new ConsumerConfig
{
    BootstrapServers = "broker.confluent.cloud:9092",
    GroupId = "workflow-engine",
    AutoOffsetReset = AutoOffsetReset.Earliest,
    SecurityProtocol = SecurityProtocol.SaslSsl,
    SaslMechanism = SaslMechanism.Plain,
    SaslUsername = "API_KEY",
    SaslPassword = "API_SECRET"
}
```

### Step 2 — Add a MessageReceived trigger in the workflow designer

In Elsa Studio, open (or create) a workflow and add the **MessageReceived** activity:

| Field | Value |
|---|---|
| **Consumer** | *My Consumer* |
| **Topics** | `orders` (the topic name to listen on) |
| **Schema Full Name** | *(leave empty to accept all messages)* |
| **Predicate** | *(leave empty)* |

Connect subsequent activities to handle the message. The trigger's output (`lastResult`) is the deserialized message body — an `ExpandoObject` whose properties mirror the JSON keys. Access fields in downstream JavaScript expressions as `lastResult.orderId`, `lastResult.amount`, etc.

The `TransportMessage` output provides the full envelope:

```javascript
// Topic the message arrived on
variables.TransportMessage.Topic

// Raw headers (Dictionary<string, byte[]>)
variables.TransportMessage.Headers

// The deserialized message body (same as lastResult)
variables.TransportMessage.Value
```

### Step 3 — Produce a reply (optional)

To send a message back, add a **ProduceMessage** activity after the trigger.

First declare a producer and a topic:

```csharp
options.Topics.Add(new TopicDefinition { Id = "replies", Name = "order-replies" });

options.Producers.Add(new ProducerDefinition
{
    Id = "my-producer",
    Name = "My Producer",
    FactoryType = typeof(ExpandoObjectProducerFactory),
    Config = new ProducerConfig { BootstrapServers = "localhost:9092" }
});
```

Then in the designer, add **ProduceMessage** with:

| Field | Value |
|---|---|
| **Topic** | *order-replies* |
| **Producer** | *My Producer* |
| **Content** | `{ "status": "accepted", "orderId": lastResult.orderId }` |

### Choosing a Consumer Factory

| Scenario | Factory |
|---|---|
| Messages are plain strings | `DefaultConsumerFactory` |
| Messages are JSON and you want dynamic access | `ExpandoObjectConsumerFactory` *(used above)* |
| Messages are JSON with a known C# model | `GenericConsumerFactory<TKey, TValue>` |
| Messages are Avro-encoded via Schema Registry | `AvroConsumerFactory` |

See [Section 5](#5-consumers--consumer-factories) for details on each factory and what the workflow receives as its output value.

---

## 1. Registration

Call `UseKafka()` on the Elsa feature builder during application startup:

```csharp
services.AddElsa(elsa =>
{
    elsa.UseKafka(kafka =>
    {
        kafka.ConfigureOptions(options =>
        {
            // populate consumers, producers, topics, schema registries …
        });
    });
});
```

All consumers start automatically via a hosted startup task (`StartConsumersStartupTask`) and are kept in sync with stored workflow triggers and bookmarks.

---

## 2. Configuration Reference

All configuration lives in `KafkaOptions`.

| Property | Type | Default | Description |
|---|---|---|---|
| `Consumers` | `ICollection<ConsumerDefinition>` | `[]` | Declared consumers |
| `Producers` | `ICollection<ProducerDefinition>` | `[]` | Declared producers |
| `Topics` | `ICollection<TopicDefinition>` | `[]` | Topics shown in dropdowns |
| `SchemaRegistries` | `ICollection<SchemaRegistryDefinition>` | `[]` | Confluent Schema Registry connections |
| `CorrelationHeaderKey` | `string` | `"x-correlation-id"` | Kafka header name for correlation IDs |
| `WorkflowInstanceIdHeaderKey` | `string` | `"x-workflow-instance-id"` | Kafka header name for workflow instance routing |
| `TenantHeaderKey` | `string` | `"Tenant"` | Kafka header name used for tenant-based trigger filtering |
| `SchemaFullNamePrefix` | `string?` | `null` | If set, the Schema Full Name dropdown only shows schemas whose full name starts with this prefix (e.g. `"Event_"`) |

---

## 3. Topics

Topics are declared to populate the `Topic` dropdown in the `ProduceMessage` activity.

```csharp
kafka.ConfigureOptions(options =>
{
    options.Topics.Add(new TopicDefinition { Id = "orders", Name = "orders" });
    options.Topics.Add(new TopicDefinition { Id = "events", Name = "events.v1" });
});
```

> The `MessageReceived` activity uses a free-text multi-input for topic names, so topics do not need to be pre-declared for consuming.

---

## 4. Schema Registries

A `SchemaRegistryDefinition` connects to a Confluent-compatible Schema Registry.  
It is required when using `AvroConsumerFactory` and powers the **Schema Full Name** dropdown in the `MessageReceived` activity.

```csharp
options.SchemaRegistries.Add(new SchemaRegistryDefinition
{
    Id = "main-registry",
    Name = "Production Registry",
    Config = new SchemaRegistryConfig
    {
        Url = "https://schema-registry.example.com",
        BasicAuthUserInfo = "key:secret"
    }
});
```

### SASL / MSK Credential Bridging

When the schema registry uses `AuthCredentialsSource.SaslInherit`, it reads credentials from the Kafka SASL config instead of a separate user/password.  
`AvroConsumerFactory` handles this automatically by merging `sasl.username` / `sasl.password` from the consumer's `ConsumerConfig` into the registry client config at runtime.

> Registries with `SaslInherit` are **skipped** in the Schema Full Name dropdown (no standalone credentials are available at design time).

---

## 5. Consumers & Consumer Factories

A `ConsumerDefinition` describes one logical Kafka consumer.  
It is assigned a `FactoryType` that controls how messages are deserialized.

```csharp
options.Consumers.Add(new ConsumerDefinition
{
    Id = "my-consumer",
    Name = "My Consumer",
    FactoryType = typeof(AvroConsumerFactory),
    SchemaRegistryId = "main-registry",       // required for Avro
    Config = new ConsumerConfig
    {
        BootstrapServers = "broker:9092",
        GroupId = "workflow-engine",
        AutoOffsetReset = AutoOffsetReset.Earliest,
        SaslMechanism = SaslMechanism.Plain,
        SecurityProtocol = SecurityProtocol.SaslSsl,
        SaslUsername = "key",
        SaslPassword = "secret"
    }
});
```

### Built-in Consumer Factories

| Factory | Key type | Value type | Use when |
|---|---|---|---|
| `DefaultConsumerFactory` | `Ignore` | `string` | Raw string messages; no JSON parsing |
| `ExpandoObjectConsumerFactory` | `Ignore` | `ExpandoObject` | JSON messages; dynamic property access in JavaScript predicates |
| `GenericConsumerFactory<TKey, TValue>` | `TKey` | `TValue` | Strongly-typed JSON with a known C# model |
| `AvroConsumerFactory` | `string` | `Dictionary<string, object?>` | Avro-encoded messages via Confluent Schema Registry |

#### Message Values in Workflows

The value exposed to the workflow (`lastResult` / `TransportMessage.Value`) depends on the factory:

- **Default** → raw `string`
- **ExpandoObject** → dynamic object; access fields as `message.fieldName` in JavaScript
- **Generic** → deserialized C# type
- **Avro** → `Dictionary<string, object?>` with field names as keys; nested records are nested dictionaries; enums are their string name; fixed bytes are `byte[]`

The `SchemaFullName` of an Avro message is available as `TransportMessage.SchemaFullName`.

---

## 6. Producers & Producer Factories

A `ProducerDefinition` describes one logical Kafka producer.

```csharp
options.Producers.Add(new ProducerDefinition
{
    Id = "my-producer",
    Name = "My Producer",
    FactoryType = typeof(ExpandoObjectProducerFactory),
    Config = new ProducerConfig
    {
        BootstrapServers = "broker:9092",
        SaslMechanism = SaslMechanism.Plain,
        SecurityProtocol = SecurityProtocol.SaslSsl,
        SaslUsername = "key",
        SaslPassword = "secret"
    }
});
```

### Built-in Producer Factories

| Factory | Serialization | Use when |
|---|---|---|
| `DefaultProducerFactory` | `string` → UTF-8 bytes | Simple string payloads |
| `ExpandoObjectProducerFactory` | `ExpandoObject` → JSON | Dynamic/anonymous objects formed in the workflow |
| `GenericProducerFactory<TKey, TValue>` | typed → JSON | Strongly-typed C# models |

---

## 7. Activities

### 7.1 MessageReceived

**Category:** Kafka  
**Type:** Trigger (starts a new workflow instance or resumes a waiting instance)

Waits for a Kafka message that matches all configured filters, then delivers the message to the workflow.

#### Inputs

| Field | Required | Description |
|---|---|---|
| **Consumer** | Yes | Dropdown — selects which `ConsumerDefinition` to subscribe with |
| **Topics** | Yes | Multi-text — one or more topic names to listen on |
| **Schema Full Name** | No | Dropdown — when selected, only Avro messages whose schema full name matches this value will fire. Requires `AvroConsumerFactory`. Leave empty (or select `(any)`) to accept all messages. |
| **Predicate** | No | JavaScript expression that receives `transportMessage` and `message` variables. Return `true` to accept the message, `false` to skip it. |
| **Local** | No | When checked, the activity only resumes for messages that were sent by the same workflow instance (matched via `x-workflow-instance-id` header). Useful for request/response patterns within a single workflow. |

#### Outputs

| Output | Type | Description |
|---|---|---|
| `Result` (`lastResult`) | `object?` | The deserialized message value (see factory table above) |
| `TransportMessage` | `KafkaTransportMessage` | Full transport envelope including `Key`, `Value`, `Topic`, `Headers`, and `SchemaFullName` |

#### Filtering Order

When a message arrives, it is matched against all registered trigger/bookmark bindings in this order:

1. **Consumer** — the message's consumer definition ID must match
2. **Topic** — the message's topic must be in the activity's topics list
3. **Tenant** — if the workflow trigger has a `TenantId`, the message must carry a matching value in the `Tenant` header (configurable via `TenantHeaderKey`). No `TenantId` = no filtering (backwards-compatible).
4. **Schema Full Name** — if set, `KafkaTransportMessage.SchemaFullName` must match exactly.
5. **Predicate** — the JavaScript expression is evaluated last.

#### Example: filter by Avro schema

In the Studio designer, set **Consumer** to an Avro consumer (one using `AvroConsumerFactory`) and pick the schema from the **Schema Full Name** dropdown. The dropdown is populated live from all configured schema registries, optionally filtered by `SchemaFullNamePrefix`.

---

### 7.2 ProduceMessage

**Category:** Kafka  
**Type:** Code Activity (fire-and-forget within the workflow)

Produces a message to a Kafka topic.

#### Inputs

| Field | Required | Description |
|---|---|---|
| **Topic** | Yes | Dropdown — selects one of the declared `TopicDefinition` entries |
| **Producer** | Yes | Dropdown — selects which `ProducerDefinition` to use |
| **Content** | Yes | The message body. Type must match the producer's factory (string, ExpandoObject, typed model). |
| **Key** | No | Optional message key. Blank strings are treated as `null`. |
| **Correlation ID** | No | Written to the `x-correlation-id` header on the produced message. |
| **Local** | No | When checked, writes the current workflow instance ID to the `x-workflow-instance-id` header so that a downstream `MessageReceived` (also set to **Local**) in the same workflow instance will exclusively receive this message. |

---

## 8. Correlation Strategies

A correlation strategy extracts a correlation ID from an incoming `KafkaTransportMessage`. The ID is used to resume waiting workflow instances by correlation.

### Built-in Strategies

| Strategy | Behaviour |
|---|---|
| `HeaderCorrelationStrategy` *(default)* | Reads the UTF-8 value of the header named by `KafkaOptions.CorrelationHeaderKey` (`x-correlation-id`) |
| `NullCorrelationStrategy` | Always returns `null`; disables correlation-based resumption |

### Registering a Custom Strategy

```csharp
elsa.UseKafka(kafka =>
{
    // By type (registered as scoped automatically):
    kafka.WithCorrelationStrategy<MyCorrelationStrategy>();

    // By factory:
    kafka.WithCorrelationStrategy(sp => new MyCorrelationStrategy(sp.GetRequiredService<IFoo>()));
});
```

Implement `ICorrelationStrategy`:

```csharp
public class MyCorrelationStrategy : ICorrelationStrategy
{
    public string? GetCorrelationId(KafkaTransportMessage transportMessage)
    {
        // return a string or null
    }
}
```

---

## 9. Multi-Tenancy

The module integrates with Elsa's multi-tenancy infrastructure. Each stored trigger carries the `TenantId` of the workflow that registered it.

### Trigger-Level Tenant Filtering

When a workflow trigger has a non-null `TenantId`, the module filters incoming messages by the value of the **Tenant header** (default header name: `Tenant`, configurable via `KafkaOptions.TenantHeaderKey`):

- Message has no `Tenant` header → trigger is **skipped**
- Message header value does not match `TenantId` → trigger is **skipped**
- Exact match → trigger **fires**

Triggers with no `TenantId` are unfiltered — they fire for every matching message regardless of tenant headers. This ensures backwards compatibility with single-tenant deployments.

### Tenant Context on Invocation

When a trigger fires, the module pushes the trigger's `TenantId` as an ambient tenant context (`ITenantAccessor.PushContext`) before calling `ITriggerInvoker.InvokeAsync`. This ensures Elsa's EF Core query filters resolve to the correct tenant's data during workflow execution.

### Changing the Tenant Header Name

```csharp
kafka.ConfigureOptions(options =>
{
    options.TenantHeaderKey = "X-Tenant-ID";
});
```

---

## 10. Extensibility

### Custom Consumer Factory

Implement `IConsumerFactory` to support any deserialization strategy (e.g. Protobuf, MessagePack, custom binary).

```csharp
public class ProtobufConsumerFactory : IConsumerFactory
{
    public IConsumer CreateConsumer(CreateConsumerContext context)
    {
        var consumer = new ConsumerBuilder<string, MyMessage>(context.ConsumerDefinition.Config)
            .SetValueDeserializer(new ProtobufDeserializer<MyMessage>())
            .Build();

        // Optional: transform the value and/or set SchemaFullName on KafkaTransportMessage.
        // The transformer runs in Worker.ProcessMessageAsync before the message is dispatched.
        return new ConsumerProxy(consumer, raw =>
        {
            if (raw is not MyMessage msg) return (raw, null);
            return (msg, MyMessage.Descriptor.FullName);
        });
    }
}
```

Register it:

```csharp
services.AddConsumerFactory<ProtobufConsumerFactory>();
```

Then reference it in a `ConsumerDefinition`:

```csharp
options.Consumers.Add(new ConsumerDefinition
{
    Id = "protobuf-consumer",
    Name = "Protobuf Consumer",
    FactoryType = typeof(ProtobufConsumerFactory),
    Config = new ConsumerConfig { … }
});
```

#### The `ValueTransformer` Contract

`ConsumerProxy` accepts an optional `Func<object?, (object? Value, string? SchemaFullName)>` delegate.  
`Worker` calls this delegate for every consumed message before building `KafkaTransportMessage`:

- Return the transformed `Value` — this is what the workflow receives as its result.
- Return a `SchemaFullName` string — this is stored on `KafkaTransportMessage.SchemaFullName` and used by the **Schema Full Name** filter on `MessageReceived`.
- Return `null` for `SchemaFullName` if schema-based filtering is not applicable.

When no transformer is set (`null`), the raw consumed value is passed through unchanged.

### Custom Producer Factory

```csharp
public class ProtobufProducerFactory : IProducerFactory
{
    public IProducer CreateProducer(CreateProducerContext context)
    {
        var producer = new ProducerBuilder<string, MyMessage>(context.ProducerDefinition.Config)
            .SetValueSerializer(new ProtobufSerializer<MyMessage>())
            .Build();
        return new ProducerProxy(producer);
    }
}
```

Register: `services.AddProducerFactory<ProtobufProducerFactory>();`

### Custom Definition Providers

By default all definitions come from `KafkaOptions` via `OptionsDefinitionProvider`.  
Implement any of the following interfaces to load definitions from a database, configuration service, or other source:

- `IConsumerDefinitionProvider`
- `IProducerDefinitionProvider`
- `ITopicDefinitionProvider`
- `ISchemaRegistryDefinitionProvider`

Register with the DI container as scoped services alongside (or instead of) the default provider.

### Custom Correlation Strategy

See [Section 8](#8-correlation-strategies).
