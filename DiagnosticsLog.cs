internal static class DiagnosticsLog
{
    private static readonly object SyncRoot = new();

    public static string FilePath => Path.Combine(AppContext.BaseDirectory, "logs", "cs2-mcp.log");

    public static void Write(string message, Exception? exception = null)
    {
        try
        {
            var directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var line = $"{DateTimeOffset.Now:O} [pid {Environment.ProcessId}] {message}{Environment.NewLine}";
            if (exception is not null)
            {
                line += exception + Environment.NewLine;
            }

            lock (SyncRoot)
            {
                using var stream = new FileStream(
                    FilePath,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.ReadWrite | FileShare.Delete);
                using var writer = new StreamWriter(stream);
                writer.Write(line);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }
        }
        catch
        {
            // Diagnostics must never break MCP stdio or the web UI.
        }
    }
}
