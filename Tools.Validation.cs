using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol;

internal static partial class Tools
{
    private static void ValidateFindQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length > 80 || !SafeFindQueryRegex().IsMatch(query))
        {
            throw new McpException("Find query must be 1-80 characters and may only contain letters, digits, spaces, underscore, dash, dot, colon, and asterisk.");
        }
    }

    private static void ValidateCommandName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 80 || !SafeCommandNameRegex().IsMatch(name))
        {
            throw new McpException("Command name must be 1-80 characters and may only contain letters, digits, underscore, and dot.");
        }
    }

    private static void ValidateMapName(string mapName)
    {
        if (string.IsNullOrWhiteSpace(mapName) || mapName.Length > 80 || !SafeMapNameRegex().IsMatch(mapName))
        {
            throw new McpException("Map name must be 1-80 characters and may only contain letters, digits, and underscore.");
        }
    }

    private static void ValidateMapFilter(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter) || filter.Length > 80 || !SafeMapFilterRegex().IsMatch(filter))
        {
            throw new McpException("Map filter must be 1-80 characters and may only contain letters, digits, underscore, dash, dot, and asterisk.");
        }
    }

    private static void ValidateBotName(string botName)
    {
        if (string.IsNullOrWhiteSpace(botName) || botName.Length > 40 || !SafeBotNameRegex().IsMatch(botName))
        {
            throw new McpException("Bot name must be 1-40 characters and may only contain letters, digits, underscore, dash, and dot.");
        }
    }

    private static string GetBotTeamCommandSuffix(string team)
    {
        return team.Trim().ToLowerInvariant() switch
        {
            "" or "any" or "auto" => string.Empty,
            "ct" or "counterterrorist" or "counter-terrorist" => "_ct",
            "t" or "terrorist" => "_t",
            _ => throw new McpException("Bot team must be one of: any, ct, t.")
        };
    }

    private static string ResolveGamemodeConfigName(string mode)
    {
        var key = NormalizeGamemodeKey(mode);
        if (GamemodeConfigs.TryGetValue(key, out var configName))
        {
            return configName;
        }

        var allowedModes = string.Join(", ", GamemodeConfigs.Keys.Order(StringComparer.OrdinalIgnoreCase));
        throw new McpException($"Unknown gamemode '{mode}'. Allowed values: {allowedModes}");
    }

    private static string NormalizeGamemodeKey(string mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
        {
            throw new McpException("Gamemode must not be empty.");
        }

        var key = mode.Trim();

        if (key.EndsWith(".cfg", StringComparison.OrdinalIgnoreCase))
        {
            key = key[..^4];
        }

        if (key.StartsWith("gamemode_", StringComparison.OrdinalIgnoreCase))
        {
            key = key["gamemode_".Length..];
        }

        return key;
    }

    private static void ValidateWorkshopId(string workshopId)
    {
        if (string.IsNullOrWhiteSpace(workshopId) ||
            workshopId.Length > 20 ||
            !WorkshopIdRegex().IsMatch(workshopId) ||
            !ulong.TryParse(workshopId, out var parsed) ||
            parsed == 0)
        {
            throw new McpException("Workshop id must be a positive unsigned integer string with up to 20 digits.");
        }
    }

    private static void ValidateConfigName(string configName)
    {
        if (string.IsNullOrWhiteSpace(configName) || configName.Length > 80 || !SafeConfigNameRegex().IsMatch(configName))
        {
            throw new McpException("Config name must be 1-80 characters and may only contain letters, digits, underscore, dash, and dot.");
        }
    }

    private static void ValidateCvarValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 120 || !SafeCvarValueRegex().IsMatch(value))
        {
            throw new McpException("Cvar value must be 1-120 characters and may only contain letters, digits, underscore, dash, dot, slash, colon, comma, and plus.");
        }
    }

    private static void ValidateSafeText(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength || !SafeTextRegex().IsMatch(value))
        {
            throw new McpException($"{parameterName} must be 1-{maxLength} printable characters and cannot contain semicolons, quotes, or newlines.");
        }
    }

    private static void EnsureAllowed(
        IConfiguration configuration,
        string value,
        string configurationKey,
        string environmentVariable,
        IReadOnlyCollection<string> defaultValues,
        string valueKind)
    {
        if (BypassAllowlists(configuration))
        {
            return;
        }

        var allowedValues = GetAllowedValueSet(configuration, configurationKey, environmentVariable, defaultValues);

        if (allowedValues.Count == 0)
        {
            throw new McpException($"No {valueKind}s are allowlisted. Configure {environmentVariable} as a comma-separated list.");
        }

        if (!allowedValues.Contains(value))
        {
            var sample = string.Join(", ", allowedValues.Take(20));
            throw new McpException($"The {valueKind} '{value}' is not allowlisted. Allowed values: {sample}");
        }
    }

    private static bool BypassAllowlists(IConfiguration configuration)
    {
        var value = configuration["CS2:BypassAllowlists"] ?? Environment.GetEnvironmentVariable("CS2_BYPASS_ALLOWLISTS");
        return IsEnabledFlag(value);
    }

    private static bool IsEnabledFlag(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "1" or "true" or "yes" or "y" or "on" => true,
            _ => false
        };
    }

    private static HashSet<string> GetAllowedValueSet(
        IConfiguration configuration,
        string configurationKey,
        string environmentVariable,
        IReadOnlyCollection<string> defaultValues)
    {
        return new HashSet<string>(
            GetAllowedValueList(configuration, configurationKey, environmentVariable, defaultValues),
            StringComparer.OrdinalIgnoreCase);
    }

    private static List<string> GetAllowedValueList(
        IConfiguration configuration,
        string configurationKey,
        string environmentVariable,
        IReadOnlyCollection<string> defaultValues)
    {
        var configured = configuration[configurationKey] ?? Environment.GetEnvironmentVariable(environmentVariable);
        var values = string.IsNullOrWhiteSpace(configured) ? defaultValues : SplitList(configured);

        return values
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<string> SplitList(string value)
    {
        return value
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(item => item.Length > 0);
    }

    [GeneratedRegex(@"^[A-Za-z0-9_ .:*\-]{1,80}$")]
    private static partial Regex SafeFindQueryRegex();

    [GeneratedRegex(@"^[A-Za-z0-9_.]{1,80}$")]
    private static partial Regex SafeCommandNameRegex();

    [GeneratedRegex(@"^[A-Za-z0-9_]{1,80}$")]
    private static partial Regex SafeMapNameRegex();

    [GeneratedRegex(@"^[A-Za-z0-9_.*\-]{1,80}$")]
    private static partial Regex SafeMapFilterRegex();

    [GeneratedRegex(@"^[A-Za-z0-9_.\-]{1,40}$")]
    private static partial Regex SafeBotNameRegex();

    [GeneratedRegex(@"^[0-9]{1,20}$")]
    private static partial Regex WorkshopIdRegex();

    [GeneratedRegex(@"^[A-Za-z0-9_.\-]{1,80}$")]
    private static partial Regex SafeConfigNameRegex();

    [GeneratedRegex(@"^[A-Za-z0-9_.\-/:,+]{1,120}$")]
    private static partial Regex SafeCvarValueRegex();

    [GeneratedRegex("^[^;\"\r\n]{1,}$")]
    private static partial Regex SafeTextRegex();
}
