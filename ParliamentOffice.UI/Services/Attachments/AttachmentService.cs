using System.IO;
using Microsoft.Data.Sqlite;
using ParliamentOffice.UI.Data;
using ParliamentOffice.UI.Services.Audit;

namespace ParliamentOffice.UI.Services.Attachments;

/// <summary>
/// Handles attachment storage paths and database registration for supported entities.
/// </summary>
public sealed class AttachmentService
{
    private readonly ActivityLogService _activityLogService;

    public AttachmentService()
        : this(new ActivityLogService())
    {
    }

    public AttachmentService(ActivityLogService activityLogService)
    {
        _activityLogService = activityLogService;
    }

    /// <summary>
    /// Resolves the storage folder for an entity while preserving the current Build010 folder layout.
    /// </summary>
    public string GetFolder(string entityType, int entityId)
    {
        return entityType switch
        {
            "Outgoing" => GetOutgoingFolder(entityId),
            "Incoming" => GetIncomingFolder(entityId),
            "Citizen" => Path.Combine(AppPaths.CitizenAttachments, entityId.ToString()),
            _ => throw new ArgumentException("Unsupported attachment entity type.", nameof(entityType))
        };
    }

    /// <summary>
    /// Copies a source file to the managed attachment folder and records it in the Attachments table.
    /// </summary>
    public string AttachFile(string entityType, int entityId, string sourceFilePath, int uploadedByUserId)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            throw new ArgumentException("Attachment file path is required.", nameof(sourceFilePath));
        }

        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("Attachment file was not found.", sourceFilePath);
        }

        var folder = GetFolder(entityType, entityId);
        Directory.CreateDirectory(folder);

        var target = Path.Combine(folder, Path.GetFileName(sourceFilePath));
        File.Copy(sourceFilePath, target, true);

        var info = new FileInfo(target);
        Db.Execute(
            @"INSERT INTO Attachments(EntityType,EntityId,FileName,FilePath,FileExtension,FileSize,UploadedBy)
VALUES($t,$id,$n,$p,$e,$s,$u)",
            new SqliteParameter("$t", entityType),
            new SqliteParameter("$id", entityId),
            new SqliteParameter("$n", info.Name),
            new SqliteParameter("$p", target),
            new SqliteParameter("$e", info.Extension),
            new SqliteParameter("$s", info.Length),
            new SqliteParameter("$u", uploadedByUserId));

        _activityLogService.TryLog(uploadedByUserId, "AttachFile", entityType, entityId, info.Name);
        return target;
    }

    /// <summary>
    /// Gets the citizen dossier id related to an attachment entity, when one exists.
    /// </summary>
    public int? GetLinkedCitizenId(string entityType, int entityId)
    {
        return entityType switch
        {
            "Citizen" => entityId,
            "Outgoing" => ToInt(Db.Scalar("SELECT CitizenId FROM OutgoingLetters WHERE Id=$id", new SqliteParameter("$id", entityId))),
            "Incoming" => ToInt(Db.Scalar("SELECT CitizenId FROM IncomingLetters WHERE Id=$id", new SqliteParameter("$id", entityId))),
            _ => null
        };
    }

    private static int? ToInt(object? value)
    {
        return value == null || value == DBNull.Value
            ? null
            : int.TryParse(value.ToString(), out var number) ? number : null;
    }

    private static string GetOutgoingFolder(int entityId)
    {
        var citizenId = ToInt(Db.Scalar("SELECT CitizenId FROM OutgoingLetters WHERE Id=$id", new SqliteParameter("$id", entityId)));
        return citizenId != null
            ? Path.Combine(AppPaths.CitizenAttachments, citizenId.Value.ToString(), "Outgoing", entityId.ToString())
            : Path.Combine(AppPaths.OutgoingAttachments, entityId.ToString());
    }

    private static string GetIncomingFolder(int entityId)
    {
        var citizenId = ToInt(Db.Scalar("SELECT CitizenId FROM IncomingLetters WHERE Id=$id", new SqliteParameter("$id", entityId)));
        return citizenId != null
            ? Path.Combine(AppPaths.CitizenAttachments, citizenId.Value.ToString(), "Incoming", entityId.ToString())
            : Path.Combine(AppPaths.IncomingAttachments, entityId.ToString());
    }
}
