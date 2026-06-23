using System.ComponentModel;
using ModelContextProtocol.Server;

internal static partial class Tools
{
    [McpServerTool(Name = "cs2_status", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Runs CS2 RCON status and returns the full raw status output. Use this for requests like all server info, all player info, full player details, raw connected players, map, hostname, version, player counts, userids, Steam IDs, ping, loss, state, and addresses.")]
    public static Task<string> GetStatus(
        Cs2RconClient rcon,
        [Description("Maximum number of response characters to return. Clamped between 1000 and 50000.")]
        int maxCharacters = 12000)
    {
        return rcon.SendCommandAsync("status", maxCharacters);
    }

    [McpServerTool(Name = "cs2_players", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Runs CS2 RCON status and returns a compact parsed player list with human/bot/max counts. Use this for simple player list requests. For all player details or raw status lines, use cs2_status or cs2_server_info.")]
    public static async Task<Cs2PlayersResult> GetPlayers(Cs2RconClient rcon)
    {
        var status = ParseStatus(await rcon.SendCommandAsync("status"));
        return new Cs2PlayersResult(status.HumanPlayers, status.Bots, status.MaxPlayers, status.Players);
    }

    [McpServerTool(Name = "cs2_server_info", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Runs CS2 RCON status and returns a parsed server snapshot plus the raw status text. Use this when the user asks for full server state, full player information, map, hostname, version, counts, and player details in one response.")]
    public static async Task<Cs2StatusSnapshot> GetServerInfo(Cs2RconClient rcon)
    {
        return ParseStatus(await rcon.SendCommandAsync("status"));
    }

    [McpServerTool(Name = "cs2_map_info", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Returns focused current map information and common map-related cvars. Use cs2_status or cs2_server_info instead when the user asks for full server/player state.")]
    public static async Task<Cs2MapInfo> GetMapInfo(Cs2RconClient rcon)
    {
        var status = ParseStatus(await rcon.SendCommandAsync("status"));
        var cvars = await rcon.SendCommandsAsync(["game_type", "game_mode", "mapgroup", "nextlevel"], 4000);

        return new Cs2MapInfo(
            status.Map,
            ExtractCvarValue("game_type", cvars["game_type"]),
            ExtractCvarValue("game_mode", cvars["game_mode"]),
            ExtractCvarValue("mapgroup", cvars["mapgroup"]),
            ExtractCvarValue("nextlevel", cvars["nextlevel"]),
            cvars);
    }

    [McpServerTool(Name = "cs2_get_cvar", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Shows the current value/help output for a specific CS2 cvar using RCON help <name>.")]
    public static async Task<Cs2CvarValue> GetCvar(
        Cs2RconClient rcon,
        [Description("Exact cvar name, for example mp_roundtime or sv_cheats.")]
        string name)
    {
        ValidateCommandName(name);
        var raw = await rcon.SendCommandAsync($"help {name}");
        return new Cs2CvarValue(name, ExtractCvarValue(name, raw), raw);
    }

    [McpServerTool(Name = "cs2_cvarlist", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Lists Counter-Strike 2 console variables and commands through RCON. The response can be very large, so it is truncated.")]
    public static Task<string> GetCvarList(
        Cs2RconClient rcon,
        [Description("Maximum number of response characters to return. Clamped between 1000 and 50000.")]
        int maxCharacters = 12000)
    {
        return rcon.SendCommandAsync("cvarlist", maxCharacters);
    }

    [McpServerTool(Name = "cs2_list_gamemodes", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Lists known CS2 gamemode presets accepted by cs2_enable_gamemode.")]
    public static IReadOnlyList<Cs2Gamemode> ListGamemodes()
    {
        return GamemodeConfigs
            .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Select(item => new Cs2Gamemode(item.Key, item.Value))
            .ToList();
    }

    [McpServerTool(Name = "cs2_list_maps", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Lists local CS2 maps using RCON maps <filter>. Defaults to maps *. The response can be large, so it is truncated.")]
    public static Task<string> ListMaps(
        Cs2RconClient rcon,
        [Description("Map filter, for example *, de_*, or cs_*. Only letters, digits, underscore, dash, dot, and asterisk are allowed.")]
        string filter = "*",
        [Description("Maximum number of response characters to return. Clamped between 1000 and 50000.")]
        int maxCharacters = 12000)
    {
        ValidateMapFilter(filter);
        return rcon.SendCommandAsync($"maps {filter}", maxCharacters);
    }

    [McpServerTool(Name = "cs2_find", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Searches CS2 console commands and variables through RCON find <query>.")]
    public static Task<string> FindCommand(
        Cs2RconClient rcon,
        [Description("Search text, for example mp_, bot, sv_cheats, or restart. Semicolons and newlines are rejected.")]
        string query,
        [Description("Maximum number of response characters to return. Clamped between 1000 and 50000.")]
        int maxCharacters = 12000)
    {
        ValidateFindQuery(query);
        return rcon.SendCommandAsync($"find {query}", maxCharacters);
    }

    [McpServerTool(Name = "cs2_help", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Shows help for a specific CS2 console command or variable through RCON help <name>.")]
    public static Task<string> GetCommandHelp(
        Cs2RconClient rcon,
        [Description("Exact command or cvar name, for example mp_restartgame or sv_cheats.")]
        string name,
        [Description("Maximum number of response characters to return. Clamped between 1000 and 50000.")]
        int maxCharacters = 12000)
    {
        ValidateCommandName(name);
        return rcon.SendCommandAsync($"help {name}", maxCharacters);
    }

    [McpServerTool(Name = "cs2_ds_workshop_listmaps", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Lists CS2 dedicated server Workshop maps using ds_workshop_listmaps. The response can be large, so it is truncated.")]
    public static Task<string> ListWorkshopMaps(
        Cs2RconClient rcon,
        [Description("Maximum number of response characters to return. Clamped between 1000 and 50000.")]
        int maxCharacters = 12000)
    {
        return rcon.SendCommandAsync("ds_workshop_listmaps", maxCharacters);
    }
}
