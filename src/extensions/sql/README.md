# Sql Extension

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
        <li><a href="#program.cs">Program.cs</a></li>
        <li><a href="#appsettings.json">Appsettings.json</a></li>
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

This package extends [Elsa Workflows](https://github.com/elsa-workflows/elsa-core) with support for **Sql**. It introduces custom activities that make it easy to integrate Sql features directly into your workflow logic.

## ✨ Key Features

- Activities: `SqlQuery`, `SqlCommand` and `SqlSingleValue`
- Automatic sql parameterization of Variables, Inputs, Outputs and other keywords to help Sql injection.
- Add-in approach to each database provider, with the ability to easily integrate other providers using the `ISqlClient` interface.
- Syntax highlighting for SQL
- `NEW` (3.6.0)  - Support for resolving JSON, POCO's, Objects and ExpandoObjects. 

---

## ⚡ Getting Started

### 📋 Prerequisites

- Elsa Workflows **V3** installed in your project.
- Access to the database (credentials, server address, etc...).

## 🛠 Installation

The following NuGet packages are available for this extension:

```bash
Elsa.Sql
Elsa.Sql.MySql
Elsa.Sql.PostgreSql
Elsa.Sql.Sqlite
Elsa.Sql.SqlServer
```

You can install the clients via NuGet:

```bash
dotnet add package Elsa.Sql.<Client>
```

## ⚙️ Configuration

### Program.cs

Register the extension in your `Program.cs` startup:

```csharp
using Elsa.Extensions;
using Elsa.Sql.Extensions;
using Elsa.Sql.MySql;
using Elsa.Sql.PostgreSql;
using Elsa.Sql.Sqlite;
using Elsa.Sql.SqlServer;

services.AddElsa(elsa =>
    {
        elsa.UseSql(options =>
            {
                options.Clients = client =>
                {
                    client.Register<MySqlClient>("MySql");
                    client.Register<PostgreSqlClient>("PostgreSql");
                    client.Register<SqliteClient>("Sqlite");
                    client.Register<SqlServerClient>("Sql Server");
                };
            })
    }
```

Note: Names within the `("")` are optional.
These are used for retrieving the client implementation and for displaying a user friendly name in Elsa Studio.
If no name is provided, the client class name will be given as the default value, i.e. `SqliteClient`.

### Appsettings.json
There are no configuration options that can be set in `appsettings.json` for this extension.

---

## 📌 Usage

Once the implementation is registered with your required clients, the activities will be ready to use, either via code or [Elsa Studio](https://github.com/elsa-workflows/elsa-studio).

## 🚀 Activities 

This extension comes with the following activities:

### SqlQuery

| Properties | Type | Description | Input/Output | Notes |
| ---------- | ---- | ----------- | --- | ----- |
| Client | string? | The client to use, populated from registered clients in `Program.cs`. | Input | - |
| ConnectionString | string? / Secret | Database connection string. Can also be from a secret. | Input | - |
| Query | string? | Query string. Expressions can also be used with `{{ }}`. | Input | - |
| Results | DataSet? | Results from executing the query. | Output | Can not be serialized. |

### SqlCommand

| Properties | Type | Description | Input/Output | Notes |
| ---------- | ---- | ----------- | --- | ----- |
| Client | string? | The client to use, populated from registered clients in `Program.cs`. | Input | - |
| ConnectionString | string? / Secret | Database connection string. Can also be from a secret. | Input | - |
| Command | string? | Command string. Expressions can also be used with `{{ }}`. | Input | - |
| Result | int? | The number of rows affected. | Output | - |

### SqlSingleValue 

| Properties | Type | Description | Input/Output | Notes |
| ---------- | ---- | ----------- | --- | ----- |
| Client | string? | The client to use, populated from registered clients in `Program.cs`. | Input | - |
| ConnectionString | string? / Secret | Database connection string. Can also be from a secret. | Input | - |
| Query | string? | Query string. Expressions can also be used with `{{ }}`. | Input | - |
| Result | object? | Result from executing the command. | Output | - |



## 🧪 Examples

### Expressions

Expressions can be used to dynamically populate the query with workflow properties.
The `SqlEvaluator` will then use the properties set in the `ISqlClient` to parameterize the query for that clients implementation.
For example, a SQL Server query:

```sql
SELECT * FROM [Users] WHERE [Name] = {{Variable.Name}} AND [Age] > {{Input.Age}};
```

Will be converted into:

```sql
SELECT * FROM [Users] WHERE [Name] = @p1 AND [Age] > @p2;
```


### Expressions

You can use Liquid-like expressions to access Input, Output and Variable values in your query.

```liquid
{{Input.<InputName>}}
{{Output.<OutputName>}}
{{Variable.<VariableName>}}
```

Expressions can also access objects with nested properties. The following objects are supported:
- POCOs
- ExpandoObject
- IDictionary
- Arrays
- Lists
- JSON objects

```liquid
{{Input.MyObjectArr[0]}}
{{Variable.MyObject.User.Id}}
{{Variable.MyObject.Users[3].Name}}
{{Activity.MyObject.Users[2].Age}}
```

There is also added support for accessing workflow related properties:
- `ActivityContext`, aliased as `Activity`
- `ExecutionContext`, aliased as `Execution`
- `ExecutionContext.Workflow`, aliased as `Workflow`

```liquid
{{LastResult}} 
{{Workflow.Identity.DefinitionId}}
{{Activity.WorkflowExecutionContext.Id}}
```

### Breaking Expression Changes in 3.6.0

As the evaluator can now use workflow properties directly, the previous hard coded expression keys have been be removed.
To access the previous key values you will need to update your expressions.

```liquid
{{Variables.<VariableName>}}        -->     {{Variable.<VariableName>}}
{{Workflow.Definition.Id}}          -->     {{Workflow.Identity.DefinitionId}}
{{Workflow.Definition.Version.Id}}  -->     {{Workflow.Identity.Id}}
{{Workflow.Definition.Version}}     -->     {{Workflow.Identity.Version}}
{{Workflow.Instance.Id}}            -->     {{Activity.WorkflowExecutionContext.Id}}
{{Correlation.Id}}                  -->     {{Activity.WorkflowExecutionContext.CorrelationId}}
```

---

## 🗺️ Planned Features

- [ ] Add contextual autocomplete
- [ ] Connection and query validation against the database
- [ ] A basic SQL designer

---

## 🚧 Limitations

### DataSet Return Type

The `SqlQuery` activity returns results as a DataSet to give developers the option to transpose the results into whatever format they need. 
However, DataSet can **not** be serialized by `System.Text.Json.JsonSerializer` and therefore the activities `Results` attribute is set to `IsSerializable = false`.

If the DataSet needs to be used by another activity it can be stored in a variable of type `object`, provided the Storage type is changed from (the default) `'Workflow Instance'` to `'Memory'`. This prevents Elsa trying to serialize it into the database after execution, which would cause the Workflow to crash. 

There is a future development idea would add a “VariableStorageDriver” feature what would enable you to store the result of a variable externally from the serialized data, but this is not implemented yet.

---

## 🆘 Troubleshooting

### Common Errors

- **`Client not showing in the Studio drop down`**  
  Ensure the Sql client provider is registered in `Program.cs` using `elsa.UseSql(options => ... )`.

---

## 🗒️ Notes & Comments
This extension was developed to add Sql functionality to Elsa Workflows.  
If you have ideas for improvement, encounter issues, or want to share how you're using it, feel free to open an issue or start a discussion.