using Microsoft.Data.Sqlite;
using ParliamentOffice.UI.Data;

namespace ParliamentOffice.UI.Services.Audit;

/// <summary>
/// Writes non-blocking audit entries to the existing ActivityLog table.
/// </summary>
public sealed class ActivityLogService
{
    /// <summary>
    /// Attempts to record an application action without interrupting the user workflow.
    /// </summary>
    public void TryLog(int? userId, string actionType, string? entityType = null, int? entityId = null, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(actionType)) return;

        try
        {
            Db.Execute(
                @"INSERT INTO ActivityLog(UserId, ActionType, EntityType, EntityId, Description)
VALUES($userId, $actionType, $entityType, $entityId, $description)",
                new SqliteParameter("$userId", (object?)userId ?? DBNull.Value),
                new SqliteParameter("$actionType", actionType.Trim()),
                new SqliteParameter("$entityType", (object?)entityType ?? DBNull.Value),
                new SqliteParameter("$entityId", (object?)entityId ?? DBNull.Value),
                new SqliteParameter("$description", (object?)description ?? DBNull.Value));
        }
        catch
        {
            // Audit logging must never break core office operations.
        }
    }
}
