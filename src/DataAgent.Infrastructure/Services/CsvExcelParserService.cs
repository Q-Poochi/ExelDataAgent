using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using DataAgent.Application.DTOs;
using DataAgent.Application.Interfaces;

namespace DataAgent.Infrastructure.Services;

public class CsvExcelParserService : IFileParserService
{
    public async Task<FilePreviewResponse> ParsePreviewAsync(Stream fileStream, string extension, int maxRows = 50, CancellationToken cancellationToken = default)
    {
        return extension.ToLowerInvariant() switch
        {
            ".csv" => await ParseCsvAsync(fileStream, maxRows, cancellationToken),
            ".xlsx" => await ParseExcelAsync(fileStream, maxRows, cancellationToken),
            _ => throw new NotSupportedException($"File extension {extension} is not supported.")
        };
    }

    private async Task<FilePreviewResponse> ParseCsvAsync(Stream stream, int maxRows, CancellationToken cancellationToken)
    {
        var response = new FilePreviewResponse();
        
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true });

        await csv.ReadAsync();
        csv.ReadHeader();
        response.Headers = csv.HeaderRecord?.ToList() ?? new List<string>();

        int rowCount = 0;
        while (await csv.ReadAsync() && rowCount < maxRows)
        {
            var row = new List<string>();
            foreach (var header in response.Headers)
            {
                row.Add(csv.GetField(header) ?? string.Empty);
            }
            response.Rows.Add(row);
            rowCount++;
        }
        
        // Count remaining for total rows roughly, or just report what we parsed.
        // Actually to get exact totalRows of CSV we'd have to read to end. I will just read to end to count, or just return an indicator.
        int totalRows = rowCount;
        while (await csv.ReadAsync())
        {
            totalRows++;
        }
        
        response.TotalRows = totalRows;
        response.ColumnTypes = DetectColumnTypes(response.Headers, response.Rows);

        return response;
    }

    private Task<FilePreviewResponse> ParseExcelAsync(Stream stream, int maxRows, CancellationToken cancellationToken)
    {
        var response = new FilePreviewResponse();
        
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null) return Task.FromResult(response);

        var firstRowUsed = worksheet.FirstRowUsed();
        var lastRowUsed = worksheet.LastRowUsed();
        if (firstRowUsed == null || lastRowUsed == null) return Task.FromResult(response);

        var columnsUsed = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        // Read headers
        for (int i = 1; i <= columnsUsed; i++)
        {
            response.Headers.Add(firstRowUsed.Cell(i).GetString());
        }

        // Read data
        var dataRows = worksheet.Rows(firstRowUsed.RowNumber() + 1, lastRowUsed.RowNumber());
        int count = 0;
        foreach (var row in dataRows)
        {
            if (count >= maxRows) break;
            
            var rowData = new List<string>();
            for (int i = 1; i <= columnsUsed; i++)
            {
                rowData.Add(row.Cell(i).Value.ToString());
            }
            response.Rows.Add(rowData);
            count++;
        }

        response.TotalRows = (worksheet.LastRowUsed()?.RowNumber() ?? firstRowUsed.RowNumber()) - firstRowUsed.RowNumber();
        response.ColumnTypes = DetectColumnTypes(response.Headers, response.Rows);

        return Task.FromResult(response);
    }

    private Dictionary<string, string> DetectColumnTypes(List<string> headers, List<List<string>> rows)
    {
        var types = new Dictionary<string, string>();
        
        for (int col = 0; col < headers.Count; col++)
        {
            var header = headers[col];
            var colValues = rows.Select(r => r.Count > col ? r[col] : string.Empty).Where(v => !string.IsNullOrWhiteSpace(v)).ToList();

            if (!colValues.Any())
            {
                types[header] = "string";
                continue;
            }

            if (colValues.All(v => bool.TryParse(v, out _)))
            {
                types[header] = "boolean";
            }
            else if (colValues.All(v => double.TryParse(v, out _)))
            {
                types[header] = "number";
            }
            else if (colValues.All(v => DateTime.TryParse(v, out _)))
            {
                types[header] = "date";
            }
            else
            {
                types[header] = "string";
            }
        }

        return types;
    }
}
