using System.Text.RegularExpressions;

internal static partial class Tools
{
    private static Cs2StatusSnapshot ParseStatus(string statusText)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var players = new List<Cs2Player>();
        int? humanPlayers = null;
        int? bots = null;
        int? maxPlayers = null;
        string? playerState = null;
        string? mapFromSpawngroup = null;

        foreach (var line in statusText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var spawngroupMatch = LoadedSpawngroupRegex().Match(line);
            if (mapFromSpawngroup is null && spawngroupMatch.Success)
            {
                mapFromSpawngroup = ExtractSpawngroupMapName(spawngroupMatch.Groups["path"].Value);
            }

            var keyValueMatch = StatusKeyValueRegex().Match(line);
            if (keyValueMatch.Success)
            {
                var key = keyValueMatch.Groups["key"].Value.Trim().ToLowerInvariant();
                var value = keyValueMatch.Groups["value"].Value.Trim();
                values[key] = value;

                if (string.Equals(key, "players", StringComparison.OrdinalIgnoreCase))
                {
                    var playersSummaryMatch = PlayersSummaryRegex().Match(value);
                    if (playersSummaryMatch.Success)
                    {
                        humanPlayers = ParseInt(playersSummaryMatch.Groups["humans"].Value);
                        bots = ParseInt(playersSummaryMatch.Groups["bots"].Value);
                        maxPlayers = ParseInt(playersSummaryMatch.Groups["max"].Value);
                        playerState = playersSummaryMatch.Groups["state"].Success ? playersSummaryMatch.Groups["state"].Value : null;
                    }
                }

                continue;
            }

            var playerMatch = StatusPlayerRegex().Match(line);
            if (playerMatch.Success)
            {
                players.Add(new Cs2Player(
                    ParseInt(playerMatch.Groups["userid"].Value) ?? 0,
                    playerMatch.Groups["name"].Value,
                    playerMatch.Groups["uniqueid"].Value,
                    playerMatch.Groups["connected"].Value,
                    ParseInt(playerMatch.Groups["ping"].Value),
                    ParseInt(playerMatch.Groups["loss"].Value),
                    playerMatch.Groups["state"].Value,
                    ParseInt(playerMatch.Groups["rate"].Value),
                    playerMatch.Groups["address"].Success ? playerMatch.Groups["address"].Value : null));

                continue;
            }

            var source2PlayerMatch = Source2StatusPlayerRegex().Match(line);
            if (source2PlayerMatch.Success)
            {
                var name = source2PlayerMatch.Groups["name"].Value;
                var userId = ParseInt(source2PlayerMatch.Groups["userid"].Value) ?? 0;

                if (userId == 65535 && string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                players.Add(new Cs2Player(
                    userId,
                    name,
                    string.Empty,
                    source2PlayerMatch.Groups["connected"].Value,
                    ParseInt(source2PlayerMatch.Groups["ping"].Value),
                    ParseInt(source2PlayerMatch.Groups["loss"].Value),
                    source2PlayerMatch.Groups["state"].Value,
                    ParseInt(source2PlayerMatch.Groups["rate"].Value),
                    source2PlayerMatch.Groups["address"].Success ? source2PlayerMatch.Groups["address"].Value : null));
            }
        }

        var map = ExtractMapName(GetValue(values, "map")) ?? mapFromSpawngroup;

        return new Cs2StatusSnapshot(
            GetValue(values, "hostname"),
            GetValue(values, "version"),
            GetValue(values, "udp/ip"),
            GetValue(values, "os") ?? GetValue(values, "os/type"),
            GetValue(values, "type"),
            map,
            humanPlayers,
            bots,
            maxPlayers,
            playerState,
            players,
            statusText);
    }

    private static string? ExtractCvarValue(string name, string raw)
    {
        var match = QuotedCvarValueRegex().Match(raw);
        if (match.Success && string.Equals(match.Groups["name"].Value, name, StringComparison.OrdinalIgnoreCase))
        {
            return match.Groups["value"].Value;
        }

        return null;
    }

    private static string? ExtractMapName(string? rawMapValue)
    {
        return rawMapValue?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
    }

    private static string? ExtractSpawngroupMapName(string rawPath)
    {
        var path = rawPath.Trim();
        var parts = path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return null;
        }

        if (string.Equals(parts[0], "maps", StringComparison.OrdinalIgnoreCase))
        {
            return parts.Length > 1 && !string.Equals(parts[1], "prefabs", StringComparison.OrdinalIgnoreCase)
                ? parts[1]
                : null;
        }

        return parts[0];
    }

    private static string? GetValue(Dictionary<string, string> values, string key)
    {
        return values.TryGetValue(key, out var value) ? value : null;
    }

    private static int? ParseInt(string value)
    {
        return int.TryParse(value, out var parsed) ? parsed : null;
    }

    [GeneratedRegex(@"^\s*(?<key>[A-Za-z_/ ]+)\s*:\s*(?<value>.+)$")]
    private static partial Regex StatusKeyValueRegex();

    [GeneratedRegex(@"(?<humans>\d+)\s+humans?,\s+(?<bots>\d+)\s+bots?\s+\((?<max>\d+)\s+max\)(?:\s+\((?<state>[^)]*)\))?", RegexOptions.IgnoreCase)]
    private static partial Regex PlayersSummaryRegex();

    [GeneratedRegex("^#\\s*(?<userid>\\d+)\\s+\"(?<name>[^\"]*)\"\\s+(?<uniqueid>\\S+)\\s+(?<connected>\\S+)\\s+(?<ping>\\d+)\\s+(?<loss>\\d+)\\s+(?<state>\\S+)(?:\\s+(?<rate>\\d+))?(?:\\s+(?<address>\\S+))?", RegexOptions.IgnoreCase)]
    private static partial Regex StatusPlayerRegex();

    [GeneratedRegex(@"^(?<userid>\d+)\s+(?<connected>\S+)\s+(?<ping>\d+)\s+(?<loss>\d+)\s+(?<state>\S+)\s+(?<rate>\d+)(?:\s+(?<address>\S+))?\s+'(?<name>[^']*)'\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex Source2StatusPlayerRegex();

    [GeneratedRegex(@"loaded\s+spawngroup\(\s*\d+\)\s*:\s*SV:\s*\[\s*\d+:\s*(?<path>[^|\]]+)", RegexOptions.IgnoreCase)]
    private static partial Regex LoadedSpawngroupRegex();

    [GeneratedRegex("\"(?<name>[^\"]+)\"\\s*=\\s*\"(?<value>[^\"]*)\"")]
    private static partial Regex QuotedCvarValueRegex();
}
