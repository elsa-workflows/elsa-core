# MQTT Extension

<details>
  <summary>📖 Table of Contents</summary>
  <ol>
    <li><a href="#overview">Overview</a></li>
    <li><a href="#features">Features</a></li>
    <li>
      <a href="#getting-started">Getting Started</a>
      <ul>
        <li><a href="#prerequisites">Prerequisites</a></li>
        <li><a href="#installation">Installation</a></li>
      </ul>
    </li>
     <li>
      <a href="#configuration">Configuration</a>
      <ul>
        <li><a href="#programcs">Program.cs</a></li>
        <li><a href="#appsettingsjson">Appsettings.json</a></li>
      </ul>
    </li>
    <li><a href="#usage">Usage</a></li>
    <li><a href="#activities">Activities</a></li>
    <li><a href="#examples">Examples</a></li>
    <li><a href="#planned-features">Planned Features</a></li>
    <li><a href="#limitations">Limitations</a></li>
    <li><a href="#troubleshooting">Troubleshooting</a></li>
    <li><a href="#notes">Notes & Comments</a></li>
  </ol>
</details>

## 🧠 Overview

This package extends [Elsa Workflows](https://github.com/elsa-workflows/elsa-core) with support for **MQTT**.
It introduces custom activities that make it easy to integrate MQTT messaging directly into your workflow logic.

## ✨ Key Features

- Activities:
  - `MqttMessageReceived`: trigger a workflow when a message is received on one or more MQTT topics
  - `PublishMqttMessage`: publish a message to a specified MQTT topic
- Wrapper around the cross-platform [MQTTnet](https://github.com/dotnet/MQTTnet) library for MQTT operations
- Supports MQTT topic wildcards (`+` single-level, `#` multi-level)
- Configurable connection settings (e.g. broker host, port, credentials, TLS options) for flexible integration with different MQTT brokers
- Supports multiple MQTT brokers and configurations within the same application

---

## ⚡ Getting Started

### 📋 Prerequisites

- Elsa Workflows **3.7** (or higher) installed in your project.
- Access to an MQTT broker (including credentials, etc.).

## 🛠 Installation

The following NuGet packages are available for this extension:

```bash
Elsa.Mqtt
```

You can install the package via NuGet:

```bash
dotnet add package Elsa.Mqtt
```

## ⚙️ Configuration

### Program.cs

Register the extension in your application startup:

```csharp
using Elsa.Extensions;
using Elsa.Mqtt.Extensions;

services.AddElsa(elsa =>
{
    elsa.UseMqtt(mqtt =>
    {
        mqtt.ConfigureOptions = options =>
        {
            options.AddDefaultConnection(new Elsa.Mqtt.Options.MqttConnectionOptions
            {
                Host = configuration.GetValue<string>("Mqtt:Host")!,
                Port = configuration.GetValue<int>("Mqtt:Port"),
                Username = configuration.GetValue<string>("Mqtt:Username"),
                Password = configuration.GetValue<string>("Mqtt:Password"),
                UseTls = configuration.GetValue<bool>("Mqtt:UseTls"),
            });
        };
    });
});
```

And configure the connection via `appsettings.json`:

```json
"Mqtt": {
  "Host": "broker.example.org",
  "Port": 1883,
  "Username": "myuser",
  "Password": "s3cr3t",
  "UseTls": false
}
```

---

## 📌 Usage

Once the extension is registered with your required implementations, the activities will be ready to use, either via code or [Elsa Studio](https://github.com/elsa-workflows/elsa-studio).

## 🚀 Activities

This extension comes with the following activities:

### `MqttMessageReceived`

A **trigger** activity that starts or resumes a workflow when a message arrives on one or more MQTT topics.

**Outcomes:** single default outcome (activity completes normally)

| Property         | Type                  | Description                                                                  | Input/Output | Notes                                                                                             |
| ---------------- | --------------------- | ---------------------------------------------------------------------------- | ------------ | ------------------------------------------------------------------------------------------------- |
| ConnectionName   | `string?`             | The name of the MQTT connection to use, as configured in the module options. | Input        | Defaults to `'Default'`.                                                                          |
| Topics           | `ICollection<string>` | The list of topic filters to subscribe to.                                   | Input        | Supports MQTT single-level (`+`) and multi-level (`#`) wildcards. At least one topic is required. |
| TransportMessage | `MqttMessage`         | The received MQTT message, containing `Topic` and `Message` properties.      | Output       | Set from the incoming workflow input when the trigger fires.                                      |

---

### `PublishMqttMessage`

An **action** activity that publishes a message to a specified MQTT topic.

**Outcomes:** `Success` / `Failure`

| Property              | Type                        | Description                                                                  | Input/Output | Notes                                                           |
| --------------------- | --------------------------- | ---------------------------------------------------------------------------- | ------------ | --------------------------------------------------------------- |
| ConnectionName        | `string?`                   | The name of the MQTT connection to use, as configured in the module options. | Input        | Defaults to `'Default'`.                                        |
| Topic                 | `string`                    | The topic to publish the message to.                                         | Input        | Required — goes to `Failure` if empty.                          |
| Message               | `string`                    | The message payload to publish.                                              | Input        | Required — goes to `Failure` if null.                           |
| QualityOfServiceLevel | `MqttQualityOfServiceLevel` | The QoS level to use when publishing.                                        | Input        | Values: `AtMostOnce` (0), `AtLeastOnce` (1), `ExactlyOnce` (2). |
| Retain                | `bool`                      | Whether the broker should retain the message for new subscribers.            | Input        | -                                                               |
| Result                | `bool`                      | Indicates whether the publish operation succeeded.                           | Output       | `true` if the broker acknowledged success, `false` otherwise.   |

---

## 🧪 Examples

### Subscribing to a topic and logging the message

```csharp
var workflow = new Sequence
{
    Activities =
    {
        new MqttMessageReceived
        {
            ConnectionName = new("Default"),
            Topics = new(new List<string> { "sensors/temperature/#" }),
        },
        new WriteLine(context =>
        {
            var msg = context.GetLastResult<MqttMessage>();
            return $"Received on {msg.Topic}: {msg.Message}";
        }),
    }
};
```

### Publishing a message

```csharp
var workflow = new Sequence
{
    Activities =
    {
        new PublishMqttMessage
        {
            ConnectionName = new("Default"),
            Topic = new("alerts/critical"),
            Message = new("High temperature detected!"),
            QualityOfServiceLevel = new(MqttQualityOfServiceLevel.AtLeastOnce),
            Retain = new(false),
        },
    }
};
```

---

## 🚧 Limitations

- Supports only a subset of MQTT features (e.g. no support for MQTT 5 user properties, subscription identifiers, or shared subscriptions)
- Does not include built-in dead-letter or error-queue handling for failed message processing
- Does not support all advanced TLS scenarios (e.g. per-connection CA certificate pinning)
- The `MqttMessage` output type exposes only `Topic` and `Message`; binary payloads are not directly supported

---

## 🆘 Troubleshooting

### Common Errors

_None reported yet._

---

## 🗺️ Planned Features

_No features planned._

---

## 🗒️ Notes & Comments

This extension was developed to add MQTT messaging functionality to Elsa Workflows.  
If you have ideas for improvement, encounter issues, or want to share how you're using it, feel free to open an issue or start a discussion!
