using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol;
using ModelContextProtocol.Server;

internal static partial class Tools
{
    [McpServerTool(Name = "cs2_say", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Sends a chat message to the CS2 server using RCON say <message>.")]
    public static Task<string> Say(
        Cs2RconClient rcon,
        [Description("Message to send to server chat. Semicolons, quotes, and newlines are rejected.")]
        string message)
    {
        ValidateSafeText(message, "Message", 200);
        return rcon.SendCommandAsync($"say {message}");
    }

    [McpServerTool(Name = "cs2_restart_game", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false)]
    [Description("Restarts the CS2 game using mp_restartgame <delaySeconds>.")]
    public static Task<string> RestartGame(
        Cs2RconClient rcon,
        [Description("Restart delay in seconds. Clamped between 0 and 60.")]
        int delaySeconds = 1)
    {
        delaySeconds = Math.Clamp(delaySeconds, 0, 60);
        return rcon.SendCommandAsync($"mp_restartgame {delaySeconds}");
    }

    [McpServerTool(Name = "cs2_warmup_start", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Starts CS2 warmup using mp_warmup_start.")]
    public static Task<string> WarmupStart(Cs2RconClient rcon)
    {
        return rcon.SendCommandAsync("mp_warmup_start");
    }

    [McpServerTool(Name = "cs2_warmup_end", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Ends CS2 warmup using mp_warmup_end.")]
    public static Task<string> WarmupEnd(Cs2RconClient rcon)
    {
        return rcon.SendCommandAsync("mp_warmup_end");
    }

    [McpServerTool(Name = "cs2_pause_match", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Pauses the CS2 match using mp_pause_match.")]
    public static Task<string> PauseMatch(Cs2RconClient rcon)
    {
        return rcon.SendCommandAsync("mp_pause_match");
    }

    [McpServerTool(Name = "cs2_unpause_match", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Unpauses the CS2 match using mp_unpause_match.")]
    public static Task<string> UnpauseMatch(Cs2RconClient rcon)
    {
        return rcon.SendCommandAsync("mp_unpause_match");
    }

    [McpServerTool(Name = "cs2_enable_overtime", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Enables CS2 overtime and configures common overtime settings.")]
    public static Task<IReadOnlyDictionary<string, string>> EnableOvertime(
        Cs2RconClient rcon,
        [Description("Rounds per overtime series. Common value is 6, meaning 3 rounds per side. Clamped between 2 and 30.")]
        int maxRounds = 6,
        [Description("Starting money for overtime halves. Common value is 10000. Clamped between 800 and 65535.")]
        int startMoney = 10000,
        [Description("Maximum overtime series. 0 means unlimited. Clamped between 0 and 30.")]
        int limit = 0)
    {
        maxRounds = Math.Clamp(maxRounds, 2, 30);
        startMoney = Math.Clamp(startMoney, 800, 65535);
        limit = Math.Clamp(limit, 0, 30);

        return rcon.SendCommandsAsync(
            [
                "mp_overtime_enable 1",
                $"mp_overtime_maxrounds {maxRounds}",
                $"mp_overtime_startmoney {startMoney}",
                $"mp_overtime_limit {limit}"
            ],
            4000);
    }

    [McpServerTool(Name = "cs2_disable_overtime", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Disables CS2 overtime using mp_overtime_enable 0.")]
    public static Task<string> DisableOvertime(Cs2RconClient rcon)
    {
        return rcon.SendCommandAsync("mp_overtime_enable 0");
    }

    [McpServerTool(Name = "cs2_enable_friendly_fire", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Enables friendly fire using mp_friendlyfire 1.")]
    public static Task<string> EnableFriendlyFire(Cs2RconClient rcon)
    {
        return rcon.SendCommandAsync("mp_friendlyfire 1");
    }

    [McpServerTool(Name = "cs2_disable_friendly_fire", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Disables friendly fire using mp_friendlyfire 0.")]
    public static Task<string> DisableFriendlyFire(Cs2RconClient rcon)
    {
        return rcon.SendCommandAsync("mp_friendlyfire 0");
    }

    [McpServerTool(Name = "cs2_set_freezetime", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Sets round freeze time using mp_freezetime <seconds>.")]
    public static Task<string> SetFreezeTime(
        Cs2RconClient rcon,
        [Description("Freeze time in seconds. Clamped between 0 and 60.")]
        int seconds)
    {
        seconds = Math.Clamp(seconds, 0, 60);
        return rcon.SendCommandAsync($"mp_freezetime {seconds}");
    }

    [McpServerTool(Name = "cs2_set_buytime", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Sets buy time using mp_buytime <seconds>.")]
    public static Task<string> SetBuyTime(
        Cs2RconClient rcon,
        [Description("Buy time in seconds. Clamped between 0 and 600.")]
        int seconds)
    {
        seconds = Math.Clamp(seconds, 0, 600);
        return rcon.SendCommandAsync($"mp_buytime {seconds}");
    }

    [McpServerTool(Name = "cs2_set_startmoney", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Sets starting money using mp_startmoney <money>.")]
    public static Task<string> SetStartMoney(
        Cs2RconClient rcon,
        [Description("Starting money. Clamped between 800 and 65535.")]
        int money)
    {
        money = Math.Clamp(money, 800, 65535);
        return rcon.SendCommandAsync($"mp_startmoney {money}");
    }

    [McpServerTool(Name = "cs2_set_roundtime", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Sets round time in minutes using mp_roundtime, mp_roundtime_defuse, and mp_roundtime_hostage.")]
    public static Task<IReadOnlyDictionary<string, string>> SetRoundTime(
        Cs2RconClient rcon,
        [Description("Round time in minutes. Clamped between 1 and 60.")]
        int minutes)
    {
        minutes = Math.Clamp(minutes, 1, 60);
        return rcon.SendCommandsAsync(
            [
                $"mp_roundtime {minutes}",
                $"mp_roundtime_defuse {minutes}",
                $"mp_roundtime_hostage {minutes}"
            ],
            4000);
    }

    [McpServerTool(Name = "cs2_set_maxrounds", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Sets the maximum number of rounds using mp_maxrounds <rounds>.")]
    public static Task<string> SetMaxRounds(
        Cs2RconClient rcon,
        [Description("Maximum rounds. Clamped between 1 and 60.")]
        int rounds)
    {
        rounds = Math.Clamp(rounds, 1, 60);
        return rcon.SendCommandAsync($"mp_maxrounds {rounds}");
    }

    [McpServerTool(Name = "cs2_enable_gamemode", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false)]
    [Description("Enables a known CS2 gamemode preset using exec gamemode_<mode>. Accepts short names like competitive_offline or full names like gamemode_competitive_offline.cfg.")]
    public static Task<string> EnableGamemode(
        Cs2RconClient rcon,
        [Description("Known gamemode key: armsrace, casual, competitive, competitive2v2, competitive2v2_offline, competitive_offline, competitive_short, competitive_tmm, cooperative, coopmission, custom, deathmatch, deathmatch_short, deathmatch_tmm, demolition, dm_freeforall, new_user_training, retakecasual, teamdeathmatch, workshop.")]
        string mode)
    {
        var configName = ResolveGamemodeConfigName(mode);
        return rcon.SendCommandAsync($"exec {configName}");
    }

    [McpServerTool(Name = "cs2_bot_add", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Adds a bot using bot_add, bot_add_ct, or bot_add_t.")]
    public static Task<string> BotAdd(
        Cs2RconClient rcon,
        [Description("Bot team: any, ct, or t.")]
        string team = "any")
    {
        var suffix = GetBotTeamCommandSuffix(team);
        return rcon.SendCommandAsync($"bot_add{suffix}");
    }

    [McpServerTool(Name = "cs2_bot_kick", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Kicks bots using bot_kick. Omit botName to kick all bots.")]
    public static Task<string> BotKick(
        Cs2RconClient rcon,
        [Description("Optional bot name. If omitted, all bots are kicked. Only letters, digits, underscore, dash, and dot are allowed.")]
        string? botName = null)
    {
        if (string.IsNullOrWhiteSpace(botName))
        {
            return rcon.SendCommandAsync("bot_kick");
        }

        ValidateBotName(botName);
        return rcon.SendCommandAsync($"bot_kick {botName}");
    }

    [McpServerTool(Name = "cs2_bot_quota", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Sets bot_quota to the requested value.")]
    public static Task<string> BotQuota(
        Cs2RconClient rcon,
        [Description("Bot quota. Clamped between 0 and 64.")]
        int quota)
    {
        quota = Math.Clamp(quota, 0, 64);
        return rcon.SendCommandAsync($"bot_quota {quota}");
    }

    [McpServerTool(Name = "cs2_bot_difficulty", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Sets bot_difficulty. Typical values are 0 easy, 1 normal, 2 hard, and 3 expert.")]
    public static Task<string> BotDifficulty(
        Cs2RconClient rcon,
        [Description("Bot difficulty. Clamped between 0 and 3.")]
        int difficulty)
    {
        difficulty = Math.Clamp(difficulty, 0, 3);
        return rcon.SendCommandAsync($"bot_difficulty {difficulty}");
    }

    [McpServerTool(Name = "cs2_change_map", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false)]
    [Description("Changes the CS2 map using changelevel <mapName>. The map must be in the allowed map list.")]
    public static Task<string> ChangeMap(
        Cs2RconClient rcon,
        IConfiguration configuration,
        [Description("Map name, for example de_dust2. Must be allowed by CS2_ALLOWED_MAPS or the default built-in allowlist.")]
        string mapName)
    {
        ValidateMapName(mapName);
        EnsureAllowed(configuration, mapName, "CS2:AllowedMaps", "CS2_ALLOWED_MAPS", DefaultAllowedMaps, "map");
        return rcon.SendCommandAsync($"changelevel {mapName}");
    }

    [McpServerTool(Name = "cs2_changelevel_next_allowed_map", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false)]
    [Description("Changes to the next map from the allowed map list, cycling after the current map from status.")]
    public static async Task<Cs2MapChangeResult> ChangeToNextAllowedMap(
        Cs2RconClient rcon,
        IConfiguration configuration)
    {
        var allowedMaps = GetAllowedValueList(configuration, "CS2:AllowedMaps", "CS2_ALLOWED_MAPS", DefaultAllowedMaps);
        if (allowedMaps.Count == 0)
        {
            throw new McpException("No maps are allowlisted. Configure CS2_ALLOWED_MAPS as a comma-separated list.");
        }

        foreach (var allowedMap in allowedMaps)
        {
            ValidateMapName(allowedMap);
        }

        var currentMap = ParseStatus(await rcon.SendCommandAsync("status")).Map;
        var currentIndex = currentMap is null
            ? -1
            : allowedMaps.FindIndex(map => string.Equals(map, currentMap, StringComparison.OrdinalIgnoreCase));
        var nextMap = allowedMaps[(currentIndex + 1 + allowedMaps.Count) % allowedMaps.Count];
        var raw = await rcon.SendCommandAsync($"changelevel {nextMap}");

        return new Cs2MapChangeResult(currentMap, nextMap, raw);
    }

    [McpServerTool(Name = "cs2_host_workshop_map", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = true)]
    [Description("Changes to a Workshop map using host_workshop_map <workshopId>. The Workshop id must be explicitly allowlisted.")]
    public static Task<string> HostWorkshopMap(
        Cs2RconClient rcon,
        IConfiguration configuration,
        [Description("Steam Workshop file id. Must be listed in CS2_ALLOWED_WORKSHOP_IDS.")]
        string workshopId)
    {
        ValidateWorkshopId(workshopId);
        EnsureAllowed(configuration, workshopId, "CS2:AllowedWorkshopIds", "CS2_ALLOWED_WORKSHOP_IDS", [], "workshop id");
        return rcon.SendCommandAsync($"host_workshop_map {workshopId}");
    }

    [McpServerTool(Name = "cs2_ds_workshop_changelevel", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = true)]
    [Description("Changes a dedicated server to a Workshop map using ds_workshop_changelevel <workshopId>. The Workshop id must be explicitly allowlisted.")]
    public static Task<string> ChangeWorkshopLevel(
        Cs2RconClient rcon,
        IConfiguration configuration,
        [Description("Steam Workshop file id. Must be listed in CS2_ALLOWED_WORKSHOP_IDS.")]
        string workshopId)
    {
        ValidateWorkshopId(workshopId);
        EnsureAllowed(configuration, workshopId, "CS2:AllowedWorkshopIds", "CS2_ALLOWED_WORKSHOP_IDS", [], "workshop id");
        return rcon.SendCommandAsync($"ds_workshop_changelevel {workshopId}");
    }

    [McpServerTool(Name = "cs2_kick_userid", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false)]
    [Description("Kicks a player by userid from CS2 RCON status using kickid <userid> [reason].")]
    public static Task<string> KickUserId(
        Cs2RconClient rcon,
        [Description("Userid from the CS2 status command, not SteamID and not player name.")]
        int userId,
        [Description("Optional kick reason. Semicolons, quotes, and newlines are rejected.")]
        string? reason = null)
    {
        if (userId <= 0)
        {
            throw new McpException("Userid must be a positive integer from the status output.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return rcon.SendCommandAsync($"kickid {userId}");
        }

        ValidateSafeText(reason, "Reason", 120);
        return rcon.SendCommandAsync($"kickid {userId} {reason}");
    }

    [McpServerTool(Name = "cs2_set_cvar", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false)]
    [Description("Sets an allowed CS2 cvar using <name> <value>. The cvar name must be explicitly allowlisted.")]
    public static Task<string> SetCvar(
        Cs2RconClient rcon,
        IConfiguration configuration,
        [Description("Cvar name. Must be listed in CS2_ALLOWED_CVARS.")]
        string name,
        [Description("New cvar value. Restricted to a conservative token character set.")]
        string value)
    {
        ValidateCommandName(name);
        ValidateCvarValue(value);
        EnsureAllowed(configuration, name, "CS2:AllowedCvars", "CS2_ALLOWED_CVARS", [], "cvar");
        return rcon.SendCommandAsync($"{name} {value}");
    }

    [McpServerTool(Name = "cs2_exec_config", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false)]
    [Description("Executes an allowlisted CS2 config using exec <configName>.")]
    public static Task<string> ExecConfig(
        Cs2RconClient rcon,
        IConfiguration configuration,
        [Description("Config file name, for example practice.cfg. Must be listed in CS2_ALLOWED_CONFIGS.")]
        string configName)
    {
        ValidateConfigName(configName);
        EnsureAllowed(configuration, configName, "CS2:AllowedConfigs", "CS2_ALLOWED_CONFIGS", [], "config");
        return rcon.SendCommandAsync($"exec {configName}");
    }
}
