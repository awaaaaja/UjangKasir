using System.IO;

namespace UjangKasir.Desktop.Helpers;

public static class ErrorLogger
{
    private static readonly object LockObject = new();

    public static void LogInfo(string message, string? context = null)
    {
        try
        {
            Write($"""
                   [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context ?? "Info"}
                   {message}

                   """);
        }
        catch
        {
            // Logging must never interrupt cashier workflows.
        }
    }

    public static void Log(Exception exception, string? context = null)
    {
        try
        {
            Write($"""
                           [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context ?? "Unhandled"}
                           {exception}

                           """);
        }
        catch
        {
            // Logging must never interrupt cashier workflows.
        }
    }

    private static void Write(string message)
    {
        var logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
        Directory.CreateDirectory(logDirectory);
        var logPath = Path.Combine(logDirectory, $"ujangkasir-{DateTime.Now:yyyyMMdd}.log");

        lock (LockObject)
        {
            File.AppendAllText(logPath, message);
        }
    }
}
