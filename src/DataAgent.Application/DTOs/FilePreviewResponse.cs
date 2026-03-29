using System.Collections.Generic;

namespace DataAgent.Application.DTOs;

public class FilePreviewResponse
{
    public List<string> Headers { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
    public int TotalRows { get; set; }
    public Dictionary<string, string> ColumnTypes { get; set; } = new();
}
