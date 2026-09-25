# LDAP Extension

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

This package extends [Elsa Workflows](https://github.com/elsa-workflows/elsa-core) with support for **LDAP**.
It introduces custom activities that make it easy to integrate LDAP features directly into your workflow logic.

## ✨ Key Features

- Activities:
    - `AddLdapEntry`: create a new entry in an LDAP directory
    - `CompareLdapEntry`: compare an LDAP entry's attribute value with a specified value (e.g. check if up-to-date)
    - `DeleteLdapEntry`: delete an entry from an LDAP directory
    - `ModifyLdapEntry`: modify an existing entry in an LDAP directory (e.g. update attributes)
    - `MoveLdapEntry`: move an LDAP entry to a new location in the directory tree (i.e. change distinguished name)
    - `SearchLdapEntry`: search for a single entry in an LDAP directory based on specified criteria (e.g. filter, base DN)
    - `SearchLdapEntries`: search for multiple entries in an LDAP directory based on specified criteria (e.g. filter, base DN)
- Wrapper around the cross-platform `System.DirectoryServices.Protocols` library for LDAP operations (supports both Windows and Linux environments)
- Configurable connection settings (e.g. server URL, credentials, SSL/TLS options) for flexible integration with different LDAP servers
- Supports multiple LDAP servers and configurations within the same application (e.g. for different environments or use cases)

---

## ⚡ Getting Started

### 📋 Prerequisites

- Elsa Workflows **3.6** (or higher) installed in your project.
- Access to an LDAP service (including credentials, etc.).

## 🛠 Installation

The following NuGet packages are available for this extension:

```bash
Elsa.Ldap
```

You can install the clients via NuGet:

```bash
dotnet add package Elsa.Ldap
```

## ⚙️ Configuration

### Program.cs

Register the extension in your application startup:

```csharp
using Elsa.Extensions;
using Elsa.Ldap.Extensions;

services.AddElsa(elsa =>
{
    elsa.UseLdap(ldap =>
    {
        ldap.ConfigureOptions = options =>
        {
            options.AddDefaultConnection(new Elsa.Ldap.Options.LdapConnectionOptions
            {
                BindDn = configuration.GetValue<string>("Ldap:BindDn"),
                BindPassword = configuration.GetValue<string>("Ldap:BindPassword"),
                Host = configuration.GetValue<string>("Ldap:Host")!,
                Port = configuration.GetValue<int>("Ldap:Port"),
                UseSsl = configuration.GetValue<bool>("Ldap:UseSsl"),
            });
        };
    });
}
```

### Appsettings.json (If applicable)
Or via `appsettings.json`:

```json
"Ldap": {
  "BindDn": "cn=myuser,dc=example,dc=org",
  "BindPassword": "s3cr3t",
  "Host": "dc.example.org",
  "Port": 389,
  "UseSsl": false
},
```

---

## 📌 Usage

Once the extension is registered with your required implementations, the activities will be ready to use, either via code or [Elsa Studio](https://github.com/elsa-workflows/elsa-studio).

## 🚀 Activities 

This extension comes with the following activities:

### `AddLdapEntry`

**Outcomes:** `Success` / `Failure`

| Properties | Type | Description | Input/Output | Notes |
| ---------- | ---- | ----------- | --- | ----- |
| ConnectionName | `string?` | The name of the LDAP connection to use, as configured in the module options. | Input | Defaults to 'Default'. |
| EntryDn | `string` | The distinguished name of the entry to add. | Input | - |
| Attributes | `IEnumerable<DirectoryAttribute>` | The list of directory attributes to set on the new entry. | Input | Uses the native `DirectoryAttribute` type. |
| Result | `bool` | Indicates whether the add operation succeeded. | Output | `true` if the result code indicates success, `false` otherwise. |

---

### `CompareLdapEntry`

**Outcomes:** `True` / `False`

| Properties | Type | Description | Input/Output | Notes |
| ---------- | ---- | ----------- | --- | ----- |
| ConnectionName | `string?` | The name of the LDAP connection to use, as configured in the module options. | Input | Defaults to 'Default'. |
| EntryDn | `string` | The distinguished name of the entry to compare. | Input | - |
| Attribute | `DirectoryAttribute` | The attribute and value to assert against the entry. | Input | The comparison succeeds if the entry contains the specified attribute with the specified value. |
| Result | `bool` | Indicates whether the comparison matched. | Output | `true` if the result code is `CompareTrue`, `false` otherwise. |

---

### `DeleteLdapEntry`

**Outcomes:** `Success` / `Failure`

| Properties | Type | Description | Input/Output | Notes |
| ---------- | ---- | ----------- | --- | ----- |
| ConnectionName | `string?` | The name of the LDAP connection to use, as configured in the module options. | Input | Defaults to 'Default'. |
| EntryDn | `string` | The distinguished name of the entry to delete. | Input | - |
| Result | `bool` | Indicates whether the delete operation succeeded. | Output | `true` if the result code indicates success, `false` otherwise. |

---

### `ModifyLdapEntry`

**Outcomes:** `Success` / `Failure`

| Properties | Type | Description | Input/Output | Notes |
| ---------- | ---- | ----------- | --- | ----- |
| ConnectionName | `string?` | The name of the LDAP connection to use, as configured in the module options. | Input | Defaults to 'Default'. |
| EntryDn | `string` | The distinguished name of the entry to modify. | Input | - |
| Modifications | `IEnumerable<DirectoryAttributeModification>` | The list of attribute modifications to apply to the entry. | Input | Uses the native `DirectoryAttributeModification` type. |
| Result | `bool` | Indicates whether the modify operation succeeded. | Output | `true` if the result code indicates success, `false` otherwise. |

---

### `MoveLdapEntry`

**Outcomes:** `Success` / `Failure`

| Properties | Type | Description | Input/Output | Notes |
| ---------- | ---- | ----------- | --- | ----- |
| ConnectionName | `string?` | The name of the LDAP connection to use, as configured in the module options. | Input | Defaults to 'Default'. |
| EntryDn | `string` | The distinguished name of the entry to move. | Input | - |
| NewParentDn | `string?` | The distinguished name of the new parent container. | Input | Leave empty to rename inside the current parent. |
| NewCn | `string` | The new common name (RDN) of the entry. | Input | - |
| Result | `bool` | Indicates whether the move/rename operation succeeded. | Output | `true` if the result code indicates success, `false` otherwise. |

---

### `SearchLdapEntry`

**Outcomes:** `Found` / `Not Found`

| Properties | Type | Description | Input/Output | Notes |
| ---------- | ---- | ----------- | --- | ----- |
| ConnectionName | `string?` | The name of the LDAP connection to use, as configured in the module options. | Input | Defaults to 'Default'. |
| BaseDn | `string` | The base distinguished name for the search. | Input | - |
| Filter | `string` | The LDAP search filter expression. | Input | If empty, no filter is applied. |
| Scope | `SearchScope` | The scope of the LDAP search. | Input | Defaults to `Base`. |
| Attributes | `string[]?` | The attributes to return in the result. | Input | Leave empty to return all attributes. |
| Result | `SearchResultEntry?` | The raw search result entry. | Output | `null` if no matching entry was found. |
| SearchResult | `Dictionary<string, string[]>` | The search result in a serializable dictionary form. | Output | Keys are attribute names, values are string arrays of attribute values. |

---

### `SearchLdapEntries`

**Outcomes:** _none_

| Properties | Type | Description | Input/Output | Notes |
| ---------- | ---- | ----------- | --- | ----- |
| ConnectionName | `string?` | The name of the LDAP connection to use, as configured in the module options. | Input | Defaults to 'Default'. |
| BaseDn | `string` | The base distinguished name for the search. | Input | - |
| Filter | `string` | The LDAP search filter expression. | Input | If empty, no filter is applied. |
| Scope | `SearchScope` | The scope of the LDAP search. | Input | Defaults to `Subtree`. |
| Attributes | `string[]?` | The attributes to return in the results. | Input | Leave empty to return all attributes. |
| Result | `IEnumerable<SearchResultEntry>` | The raw search result entries. | Output | - |
| SearchResults | `IEnumerable<Dictionary<string, string[]>>` | The search results in serializable dictionary form. | Output | Each dictionary maps attribute names to their string array values. |

---

## 🧪 Examples

### Example of your feature

ℹ️ (List some code snippets / picture examples of how to use aspects of your extension to help others learn how to use them.) ℹ️

---

## 🚧 Limitations

ℹ️ (Bullet point known limitations, if any. For example:) ℹ️

- Does not support all LDAP operations (e.g. password management, schema modifications, etc.)
- Does not support advanced LDAP features (e.g. referrals, paged results, etc.)
- Does not include built-in error handling or retry logic
- Does not support all configuration options for LDAP connections (e.g. connection pooling, timeouts, etc.)
- Expects `System.DirectoryServices.Protocols` types as input and output for activities, which may require additional mapping or conversion in some cases

---

## 🆘 Troubleshooting

### Common Errors

ℹ️ (Bullet points known errors, if any. For example:) ℹ️

_None reported yet._

---

## 🗺️ Planned Features

ℹ️ (Checkbox points for planned features, if any. For example:) ℹ️

_No features planned._

---

## 🗒️ Notes & Comments

This extension was developed to add LDAP functionality to Elsa Workflows.  
If you have ideas for improvement, encounter issues, or want to share how you're using it, feel free to open an issue or start a discussion!