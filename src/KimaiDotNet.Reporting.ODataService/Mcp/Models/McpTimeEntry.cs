namespace MarkZither.KimaiDotNet.Reporting.ODataService.Mcp.Models;

public record McpTimeEntry(
    int Id,
    string Begin,
    string End,
    int Duration,
    string Project,
    string Activity,
    string? Description);
