using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using UjangKasir.Desktop.Helpers;

namespace UjangKasir.Desktop.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("SQLite")
            ?? "Data Source=Data/ujangkasir.db";

        var normalizedConnectionString = DatabasePathHelper.BuildSqliteConnectionString(connectionString);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(normalizedConnectionString)
            .Options;

        return new AppDbContext(options);
    }
}
