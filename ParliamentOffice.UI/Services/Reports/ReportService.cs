using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using ParliamentOffice.UI.Data;
using ParliamentOffice.UI.Services.Audit;

namespace ParliamentOffice.UI.Services.Reports;

/// <summary>
/// Creates HTML reports using the existing report queries and output folder.
/// </summary>
public sealed class ReportService
{
    private readonly ActivityLogService _activityLogService;

    public ReportService()
        : this(new ActivityLogService())
    {
    }

    public ReportService(ActivityLogService activityLogService)
    {
        _activityLogService = activityLogService;
    }

    /// <summary>
    /// Runs a predefined report and opens the generated HTML file.
    /// </summary>
    public string RunReport(string kind, string title, int generatedByUserId)
    {
        var report = QueryReport(kind);
        var file = CreateHtmlReport(title, report);
        _activityLogService.TryLog(generatedByUserId, "GenerateReport", "Report", null, kind);
        return file;
    }

    /// <summary>
    /// Returns the data table for a predefined report kind.
    /// </summary>
    public DataTable QueryReport(string kind)
    {
        return kind switch
        {
            "outgoing" => Db.Query("SELECT OutgoingNumber AS 'Ø±Ù‚Ù… Ø§Ù„ØµØ§Ø¯Ø±', OutgoingDate AS 'Ø§Ù„ØªØ§Ø±ÙŠØ®', Subject AS 'Ø§Ù„Ù…ÙˆØ¶ÙˆØ¹', Status AS 'Ø§Ù„Ø­Ø§Ù„Ø©' FROM OutgoingLetters ORDER BY Id DESC"),
            "incoming" => Db.Query("SELECT IncomingNumber AS 'Ø±Ù‚Ù… Ø§Ù„ÙˆØ§Ø±Ø¯', IncomingDate AS 'Ø§Ù„ØªØ§Ø±ÙŠØ®', Subject AS 'Ø§Ù„Ù…ÙˆØ¶ÙˆØ¹', Status AS 'Ø§Ù„Ø­Ø§Ù„Ø©' FROM IncomingLetters ORDER BY Id DESC"),
            "citizens" => Db.Query("SELECT FullName AS 'Ø§Ø³Ù… Ø§Ù„Ù…ÙˆØ§Ø·Ù†', Phone AS 'Ø§Ù„Ù‡Ø§ØªÙ', RequestSubject AS 'Ù…ÙˆØ¶ÙˆØ¹ Ø§Ù„Ø·Ù„Ø¨', Status AS 'Ø§Ù„Ø­Ø§Ù„Ø©' FROM Citizens ORDER BY Id DESC"),
            _ => Db.Query(@"SELECT 'ØµØ§Ø¯Ø±' AS 'Ø§Ù„Ù†ÙˆØ¹', OutgoingNumber AS 'Ø§Ù„Ø±Ù‚Ù…', OutgoingDate AS 'Ø§Ù„ØªØ§Ø±ÙŠØ®', Subject AS 'Ø§Ù„Ù…ÙˆØ¶ÙˆØ¹', Status AS 'Ø§Ù„Ø­Ø§Ù„Ø©' FROM OutgoingLetters UNION ALL SELECT 'ÙˆØ§Ø±Ø¯', IncomingNumber, IncomingDate, Subject, Status FROM IncomingLetters UNION ALL SELECT 'Ù…ÙˆØ§Ø·Ù†', CAST(Id AS TEXT), CreatedAt, RequestSubject, Status FROM Citizens ORDER BY 4 DESC")
        };
    }

    /// <summary>
    /// Creates an HTML report file from a data table and opens it with the default system handler.
    /// </summary>
    public string CreateHtmlReport(string title, DataTable table)
    {
        Directory.CreateDirectory(AppPaths.Reports);
        var file = Path.Combine(AppPaths.Reports, $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.html");
        var sb = new StringBuilder();

        sb.Append("<!doctype html><html lang='ar' dir='rtl'><head><meta charset='utf-8'><title>")
            .Append(WebUtility.HtmlEncode(title))
            .Append("</title><style>body{font-family:Tahoma,Arial;margin:30px}h1{text-align:center}table{width:100%;border-collapse:collapse}th{background:#111827;color:white}td,th{border:1px solid #999;padding:8px;text-align:right}tr:nth-child(even){background:#f3f4f6}@media print{button{display:none}}</style></head><body><button onclick='print()'>Ø·Ø¨Ø§Ø¹Ø©</button><h1>")
            .Append(WebUtility.HtmlEncode(title))
            .Append("</h1><table><tr>");

        foreach (DataColumn column in table.Columns)
        {
            sb.Append("<th>").Append(WebUtility.HtmlEncode(column.ColumnName)).Append("</th>");
        }

        sb.Append("</tr>");

        foreach (DataRow row in table.Rows)
        {
            sb.Append("<tr>");
            foreach (var value in row.ItemArray)
            {
                sb.Append("<td>").Append(WebUtility.HtmlEncode(value?.ToString())).Append("</td>");
            }

            sb.Append("</tr>");
        }

        sb.Append("</table></body></html>");
        File.WriteAllText(file, sb.ToString(), Encoding.UTF8);
        Process.Start(new ProcessStartInfo(file) { UseShellExecute = true });
        return file;
    }
}
