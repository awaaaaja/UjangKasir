using System.IO;
using Microsoft.Data.Sqlite;

namespace UjangKasir.Desktop.Helpers;

public static class DatabasePathHelper
{
    public static string BuildSqliteConnectionString(string configuredConnectionString)
    {
        var builder = new SqliteConnectionStringBuilder(configuredConnectionString);

        if (string.IsNullOrWhiteSpace(builder.DataSource) || !Path.IsPathFullyQualified(builder.DataSource))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dbFolder = Path.Combine(appData, "UjangKasir", "Data");
            Directory.CreateDirectory(dbFolder);
            builder.DataSource = Path.Combine(dbFolder, "ujangkasir.db");
        }
        else
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(builder.DataSource));
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        return builder.ToString();
    }

    public static string GetDataSource(string connectionString)
    {
        return Path.GetFullPath(new SqliteConnectionStringBuilder(connectionString).DataSource);
    }
}
