using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using Microsoft.AspNetCore.Http;

namespace Elsa.Data.Csv.Activities;

/// <summary>
/// Read a CSV file.
/// </summary>
[Activity("Elsa", "Data", "Read a CSV file.", Kind = ActivityKind.Task)]
public class ReadCsv : Activity
{
    /// <inheritdoc />
    public ReadCsv([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <summary>
    /// The CSV data. Can be a byte array, stream, string (CSV content or URL), or IFormFile.
    /// </summary>
    [Input(Description = "The CSV data. Can be a byte array, stream, string (CSV content or URL), or IFormFile.")]
    public Input<object?> CsvData { get; set; } = null!;

    /// <summary>
    /// Indicates whether the CSV text contains a header row as the first line.
    /// </summary>
    [Input(
        Description = "Indicates whether the CSV contains a header row as the first line.", 
        UIHint = InputUIHints.Checkbox,
        DefaultValue = true
    )] 
    public Input<bool> HasHeaderRecord { get; set; } = new(true);

    /// <summary>
    /// Optional record type to map rows to using CsvHelper's strongly-typed mapping.
    /// </summary>
    [Input(Description = "Optional record type to map rows to using CsvHelper's strongly-typed mapping.",
        UIHint = InputUIHints.TypePicker
    )]
    public Input<Type?> RecordType { get; set; } = new(default(Type?));

    /// <summary>
    /// The field delimiter used to separate values. Defaults to comma (,).
    /// </summary>
    [Input(Description = "The field delimiter used to separate values. Defaults to comma (,).", UIHint = InputUIHints.SingleLine, DefaultValue = ",")]
    public Input<string?> Delimiter { get; set; } = new(",");

    /// <summary>
    /// The parsed CSV records as a list of dictionaries.
    /// </summary>
    [Output(Description = "The parsed CSV records as a list of dictionaries.")]
    public Output<IList<object>> Records { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var csvText = await GetCsvTextAsync(context);
        var hasHeader = HasHeaderRecord.GetOrDefault<bool>(context, () => true);
        var recordType = RecordType.GetOrDefault(context);
        var delimiter = NormalizeDelimiter(Delimiter.GetOrDefault(context) ?? ",");
        var records = ParseCsv(csvText, hasHeader, recordType, delimiter);
        Records.Set(context, records);
        await context.CompleteActivityAsync();
    }

    private async Task<string> GetCsvTextAsync(ActivityExecutionContext context)
    {
        var data = CsvData.GetOrDefault(context);

        if (data == null)
            return "";

        var cancellationToken = context.CancellationToken;

        switch (data)
        {
            case byte[] bytes:
                return Encoding.UTF8.GetString(bytes);
            case Stream stream:
                {
                    using var reader = new StreamReader(stream);
                    return await reader.ReadToEndAsync(cancellationToken);
                }
            case IFormFile formFile:
                {
                    await using var stream = formFile.OpenReadStream();
                    using var reader = new StreamReader(stream);
                    return await reader.ReadToEndAsync(cancellationToken);
                }
            case string str:
                return str;
            default:
                return data.ToString() ?? "";
        }
    }

    private static string NormalizeDelimiter(string? delimiter)
    {
        if (string.IsNullOrWhiteSpace(delimiter))
            return ",";

        return delimiter switch
        {
            "\\t" => "\t",
            "tab" => "\t",
            _ => delimiter
        };
    }

    private List<object> ParseCsv(string? csvText, bool hasHeader, Type? recordType, string delimiter)
    {
        var records = new List<object>();

        using var reader = new StringReader(csvText ?? string.Empty);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = hasHeader,
            Delimiter = string.IsNullOrEmpty(delimiter) ? "," : delimiter
        };
        using var csv = new CsvReader(reader, config);

        if (hasHeader)
        {
            if (csv.Read())
                csv.ReadHeader();
        }

        if (recordType != null)
        {
            return csv.GetRecords(recordType).ToList();
        }

        while (csv.Read())
        {
            var record = new Dictionary<string, object?>();

            if (hasHeader && csv.HeaderRecord != null)
            {
                foreach (var header in csv.HeaderRecord)
                    record[header] = csv.GetField(header);
            }
            else
            {
                var fieldCount = csv.Parser.Count;
                for (var i = 0; i < fieldCount; i++)
                {
                    var key = $"Field{i + 1}";
                    record[key] = csv.GetField(i);
                }
            }

            records.Add(record);
        }

        return records;
    }
}