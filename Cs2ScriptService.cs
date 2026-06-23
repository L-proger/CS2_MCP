using Microsoft.Extensions.Configuration;
using ModelContextProtocol;

internal sealed class Cs2ScriptService
{
    private const string ProjectFileName = "CS2_MCP.csproj";

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cfg"
    };

    private const int MaximumCommandsPerScript = 200;
    private const int MaximumCommandLength = 1000;
    private const int MaximumScriptCharacters = 64000;

    private readonly IConfiguration _configuration;
    private readonly Cs2RconClient _rcon;

    public Cs2ScriptService(IConfiguration configuration, Cs2RconClient rcon)
    {
        _configuration = configuration;
        _rcon = rcon;
    }

    public IReadOnlyList<Cs2ScriptInfo> ListScripts()
    {
        var directory = EnsureScriptsDirectoryExists();
        DiagnosticsLog.Write($"Listing scripts from '{directory}'.");

        return Directory
            .EnumerateFiles(directory)
            .Where(IsSupportedScriptFile)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .Select(file => new Cs2ScriptInfo(
                Path.GetFileName(file),
                Path.GetFileNameWithoutExtension(file),
                ReadCommands(file).Count))
            .ToList();
    }

    public string EnsureScriptsDirectoryExists()
    {
        var directory = GetScriptsDirectory();
        Directory.CreateDirectory(directory);

        return directory;
    }

    public async Task<Cs2ScriptRunResult> RunScriptAsync(string scriptFileName)
    {
        var scriptPath = ResolveScriptPath(scriptFileName);
        var commands = ReadCommands(scriptPath);

        if (commands.Count == 0)
        {
            throw new McpException($"Script '{scriptFileName}' has no executable commands.");
        }

        if (commands.Count > MaximumCommandsPerScript)
        {
            throw new McpException($"Script '{scriptFileName}' has {commands.Count} commands; maximum is {MaximumCommandsPerScript}.");
        }

        var results = await _rcon.SendCommandSequenceAsync(commands, 12000);
        var fileName = Path.GetFileName(scriptPath);

        return new Cs2ScriptRunResult(
            fileName,
            Path.GetFileNameWithoutExtension(fileName),
            commands.Count,
            results);
    }

    public Cs2ScriptContent ReadScript(string scriptFileName)
    {
        var scriptPath = ResolveExistingScriptPath(scriptFileName);
        var content = File.ReadAllText(scriptPath);
        var fileName = Path.GetFileName(scriptPath);

        return new Cs2ScriptContent(
            fileName,
            Path.GetFileNameWithoutExtension(fileName),
            content,
            ReadCommandsFromContent(content).Count);
    }

    public Cs2ScriptContent SaveScript(string scriptFileName, string content)
    {
        DiagnosticsLog.Write($"Saving script request: '{scriptFileName}'.");
        var scriptPath = ResolveScriptPathForSave(scriptFileName);
        DiagnosticsLog.Write($"Resolved script path: '{scriptPath}'.");
        ValidateScriptContent(content);
        DiagnosticsLog.Write($"Writing script '{Path.GetFileName(scriptPath)}' ({content.Length} chars).");

        File.WriteAllText(scriptPath, NormalizeLineEndings(content));
        DiagnosticsLog.Write($"Wrote script '{Path.GetFileName(scriptPath)}'.");

        var fileName = Path.GetFileName(scriptPath);
        var result = new Cs2ScriptContent(
            fileName,
            Path.GetFileNameWithoutExtension(fileName),
            File.ReadAllText(scriptPath),
            ReadCommands(scriptPath).Count);

        DiagnosticsLog.Write($"Saved script '{fileName}' with {result.CommandCount} executable commands.");
        return result;
    }

    public Cs2ScriptDeleteResult DeleteScript(string scriptFileName)
    {
        DiagnosticsLog.Write($"Deleting script request: '{scriptFileName}'.");
        var scriptPath = ResolveExistingScriptPath(scriptFileName);
        var fileName = Path.GetFileName(scriptPath);

        File.Delete(scriptPath);

        DiagnosticsLog.Write($"Deleted script '{fileName}'.");
        return new Cs2ScriptDeleteResult(
            fileName,
            Path.GetFileNameWithoutExtension(fileName));
    }

    private string ResolveExistingScriptPath(string scriptFileName)
    {
        var scriptPath = ResolveScriptPath(scriptFileName);

        if (!File.Exists(scriptPath))
        {
            throw new McpException($"Script '{Path.GetFileName(scriptPath)}' was not found.");
        }

        return scriptPath;
    }

    private string ResolveScriptPathForSave(string scriptFileName)
    {
        return ResolveScriptPath(scriptFileName);
    }

    private string ResolveScriptPath(string scriptFileName)
    {
        if (string.IsNullOrWhiteSpace(scriptFileName))
        {
            throw new McpException("Script file name must not be empty.");
        }

        var cleanFileName = NormalizeScriptFileName(scriptFileName);
        if (!string.Equals(cleanFileName, NormalizeScriptFileName(cleanFileName), StringComparison.Ordinal))
        {
            throw new McpException("Script file name must not contain path separators.");
        }

        if (!SupportedExtensions.Contains(Path.GetExtension(cleanFileName)))
        {
            throw new McpException("Script file extension must be .cfg.");
        }

        var directory = GetScriptsDirectory();
        Directory.CreateDirectory(directory);

        var directoryPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        var scriptPath = Path.GetFullPath(Path.Combine(directoryPath, cleanFileName));

        if (!scriptPath.StartsWith(directoryPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new McpException("Script path escaped the scripts directory.");
        }

        return scriptPath;
    }

    private static string NormalizeScriptFileName(string scriptFileName)
    {
        var trimmed = scriptFileName.Trim();
        var cleanFileName = Path.GetFileName(trimmed);

        if (!string.Equals(cleanFileName, trimmed, StringComparison.Ordinal))
        {
            throw new McpException("Script file name must not contain path separators.");
        }

        if (cleanFileName is "." or ".." || cleanFileName.StartsWith(".", StringComparison.Ordinal))
        {
            throw new McpException("Script file name is not allowed.");
        }

        if (cleanFileName.Length > 80)
        {
            throw new McpException("Script file name must be 80 characters or fewer.");
        }

        if (cleanFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new McpException("Script file name contains invalid characters.");
        }

        if (cleanFileName.Contains('/') || cleanFileName.Contains('\\'))
        {
            throw new McpException("Script file name must not contain path separators.");
        }

        if (string.IsNullOrWhiteSpace(Path.GetExtension(cleanFileName)))
        {
            cleanFileName += ".cfg";
        }

        return cleanFileName;
    }

    private string GetScriptsDirectory()
    {
        var configured = _configuration["CS2:Scripts:Directory"] ??
            Environment.GetEnvironmentVariable("CS2_SCRIPTS_DIR");

        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var projectRoot = FindProjectRoot(AppContext.BaseDirectory);
        return Path.Combine(projectRoot ?? AppContext.BaseDirectory, "scripts");
    }

    private static string? FindProjectRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, ProjectFileName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static bool IsSupportedScriptFile(string file)
    {
        return SupportedExtensions.Contains(Path.GetExtension(file));
    }

    private static List<string> ReadCommands(string file)
    {
        return ReadCommandsFromContent(File.ReadAllText(file));
    }

    private static List<string> ReadCommandsFromContent(string content)
    {
        return content
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .Where(line => !line.StartsWith("#", StringComparison.Ordinal))
            .Where(line => !line.StartsWith("//", StringComparison.Ordinal))
            .Select(ValidateCommand)
            .ToList();
    }

    private static void ValidateScriptContent(string content)
    {
        if (content.Length > MaximumScriptCharacters)
        {
            throw new McpException($"Script content is too large; maximum is {MaximumScriptCharacters} characters.");
        }

        var commands = ReadCommandsFromContent(content);
        if (commands.Count > MaximumCommandsPerScript)
        {
            throw new McpException($"Script has {commands.Count} commands; maximum is {MaximumCommandsPerScript}.");
        }
    }

    private static string NormalizeLineEndings(string content)
    {
        return content.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", Environment.NewLine);
    }

    private static string ValidateCommand(string command)
    {
        if (command.Length > MaximumCommandLength)
        {
            throw new McpException($"Script command is too long; maximum is {MaximumCommandLength} characters.");
        }

        if (command.Any(char.IsControl))
        {
            throw new McpException("Script commands must not contain control characters.");
        }

        return command;
    }
}
