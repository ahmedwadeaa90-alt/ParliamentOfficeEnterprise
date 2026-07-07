using System.Data;
using Microsoft.Data.Sqlite;

namespace ParliamentOffice.UI.Data;

public static class Db
{
    public static int Execute(string sql, params SqliteParameter[] parameters)
    {
        using var connection = new SqliteConnection(AppPaths.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddRange(parameters);
        return command.ExecuteNonQuery();
    }

    public static object? Scalar(string sql, params SqliteParameter[] parameters)
    {
        using var connection = new SqliteConnection(AppPaths.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddRange(parameters);
        return command.ExecuteScalar();
    }

    public static DataTable Query(string sql, params SqliteParameter[] parameters)
    {
        using var connection = new SqliteConnection(AppPaths.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddRange(parameters);
        using var reader = command.ExecuteReader();
        var table = new DataTable();
        table.Load(reader);
        return table;
    }
}
