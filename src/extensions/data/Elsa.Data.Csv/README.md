# CSV Extension

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
      </ul>
    </li>
    <li><a href="#usage">Usage</a></li>
    <li><a href="#activities">Activities</a></li>
    <li><a href="#examples">Examples</a></li>
    <li><a href="#limitations">Limitations</a></li>
    <li><a href="#troubleshooting">Troubleshooting</a></li>
    <li><a href="#notes">Notes & Comments</a></li>
  </ol>
</details>

## 🧠 Overview

This package extends [Elsa Workflows](https://github.com/elsa-workflows/elsa-core) with support for **CSV processing**. It introduces custom activities that make it easy to read and parse CSV data directly into your workflow logic.

## ✨ Key Features

- Activity: `ReadCsv`
- Support for multiple input formats (byte arrays, streams, strings, URLs, and uploaded files)
- Configurable delimiters and header row detection
- Optional strongly-typed mapping using CsvHelper
- Output as dictionaries or custom types

---

## ⚡ Getting Started

### 📋 Prerequisites

- Elsa Workflows **V3** installed in your project.

## 🛠 Installation

The following NuGet package is available for this extension:

```
Elsa.Data.Csv
```

You can install the package via NuGet:

```bash
dotnet add package Elsa.Data.Csv
```

## ⚙️ Configuration

### Program.cs

Register the extension in your application startup:

```csharp
using Elsa.Extensions;

services.AddElsa(elsa =>
{
    elsa.UseCsv();
});
```

---

## 📌 Usage

Once the extension is registered, the CSV activities will be ready to use, either via code or [Elsa Studio](https://github.com/elsa-workflows/elsa-studio).

## 🚀 Activities

This extension comes with the following activity:

### ReadCsv

Reads and parses CSV data from various sources.

| Properties | Type | Description | Input/Output | Notes |
| ---------- | ---- | ----------- | ------------ | ----- |
| CsvData | object? | The CSV data to process | Input | Can be a byte array, stream, string (CSV content or URL), or IFormFile |
| HasHeaderRecord | bool | Indicates whether the CSV contains a header row as the first line | Input | Default: true |
| RecordType | Type? | Optional record type to map rows to using CsvHelper's strongly-typed mapping | Input | Use for strongly-typed object mapping |
| Delimiter | string? | The field delimiter used to separate values | Input | Default: "," (comma). Also supports "\\t" or "tab" for tab-separated values |
| Records | IList<object> | The parsed CSV records as a list of dictionaries or typed objects | Output | - |

---

## 🧪 Examples

### Reading CSV from a string

Configure the `ReadCsv` activity with CSV content as a string input:
- Set `CsvData` to your CSV string
- Set `HasHeaderRecord` to `true` if your CSV has headers
- Access the parsed records from the `Records` output

### Reading CSV with custom delimiter

For tab-separated or other delimited files:
- Set `Delimiter` to `"\\t"` or `"tab"` for tab-separated values
- Set `Delimiter` to any custom character (e.g., `";"` for semicolon-separated)

### Strongly-typed mapping

To map CSV rows to custom objects:
- Set `RecordType` to your desired .NET type
- The activity will use CsvHelper's mapping to create strongly-typed objects
- Access the typed records from the `Records` output

---

## 🚧 Limitations

- CSV parsing uses CsvHelper library conventions and configuration
- Large CSV files may impact memory usage as all records are loaded into memory

---

## 🆘 Troubleshooting

### Common Errors

- **Empty or null output**

  Ensure the `CsvData` input contains valid CSV content and is not null or empty.

- **Incorrect parsing**
  
  Verify that the `Delimiter` setting matches your CSV file format and that `HasHeaderRecord` is set correctly.

- **Type mapping errors**

  When using `RecordType`, ensure your type's properties match the CSV column names (or use CsvHelper mapping attributes).

---

## 🗒️ Notes & Comments

This extension was developed to add CSV processing functionality to Elsa Workflows using the CsvHelper library.
If you have ideas for improvement, encounter issues, or want to share how you're using it, feel free to open an issue or start a discussion!
