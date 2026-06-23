internal sealed record Cs2StatusSnapshot(
    string? Hostname,
    string? Version,
    string? UdpIp,
    string? Os,
    string? Type,
    string? Map,
    int? HumanPlayers,
    int? Bots,
    int? MaxPlayers,
    string? PlayerState,
    IReadOnlyList<Cs2Player> Players,
    string Raw);

internal sealed record Cs2PlayersResult(
    int? HumanPlayers,
    int? Bots,
    int? MaxPlayers,
    IReadOnlyList<Cs2Player> Players);

internal sealed record Cs2Player(
    int UserId,
    string Name,
    string UniqueId,
    string Connected,
    int? Ping,
    int? Loss,
    string? State,
    int? Rate,
    string? Address);

internal sealed record Cs2CvarValue(
    string Name,
    string? Value,
    string Raw);

internal sealed record Cs2MapInfo(
    string? CurrentMap,
    string? GameType,
    string? GameMode,
    string? MapGroup,
    string? NextLevel,
    IReadOnlyDictionary<string, string> RawCvars);

internal sealed record Cs2MapChangeResult(
    string? PreviousMap,
    string NextMap,
    string Raw);

internal sealed record Cs2Gamemode(
    string Key,
    string ConfigName);

internal sealed record Cs2CommandResult(
    string Command,
    string Response);

internal sealed record Cs2ScriptInfo(
    string FileName,
    string Name,
    int CommandCount);

internal sealed record Cs2ScriptContent(
    string FileName,
    string Name,
    string Content,
    int CommandCount);

internal sealed record Cs2ScriptDeleteResult(
    string FileName,
    string Name);

internal sealed record Cs2ScriptRunResult(
    string FileName,
    string Name,
    int CommandCount,
    IReadOnlyList<Cs2CommandResult> Results);
