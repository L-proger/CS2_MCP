using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;

[McpServerToolType]
internal static partial class Tools
{
    private static readonly IReadOnlyDictionary<string, string> GamemodeConfigs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["armsrace"] = "gamemode_armsrace",
        ["casual"] = "gamemode_casual",
        ["competitive"] = "gamemode_competitive",
        ["competitive2v2"] = "gamemode_competitive2v2",
        ["competitive2v2_offline"] = "gamemode_competitive2v2_offline",
        ["competitive_offline"] = "gamemode_competitive_offline",
        ["competitive_short"] = "gamemode_competitive_short",
        ["competitive_tmm"] = "gamemode_competitive_tmm",
        ["cooperative"] = "gamemode_cooperative",
        ["coopmission"] = "gamemode_coopmission",
        ["custom"] = "gamemode_custom",
        ["deathmatch"] = "gamemode_deathmatch",
        ["deathmatch_short"] = "gamemode_deathmatch_short",
        ["deathmatch_tmm"] = "gamemode_deathmatch_tmm",
        ["demolition"] = "gamemode_demolition",
        ["dm_freeforall"] = "gamemode_dm_freeforall",
        ["new_user_training"] = "gamemode_new_user_training",
        ["retakecasual"] = "gamemode_retakecasual",
        ["teamdeathmatch"] = "gamemode_teamdeathmatch",
        ["workshop"] = "gamemode_workshop"
    };

    private static readonly string[] DefaultAllowedMaps =
    [
        "de_ancient",
        "de_anubis",
        "de_cache",
        "de_dust2",
        "de_inferno",
        "de_mirage",
        "de_nuke",
        "de_overpass",
        "de_train",
        "de_vertigo",
        "cs_italy",
        "cs_office"
    ];

    internal static IReadOnlyList<string> GetConfiguredAllowedMaps(IConfiguration configuration)
    {
        return GetAllowedValueList(configuration, "CS2:AllowedMaps", "CS2_ALLOWED_MAPS", DefaultAllowedMaps);
    }

    internal static void ValidateAllowedMap(IConfiguration configuration, string mapName)
    {
        ValidateMapName(mapName);
        EnsureAllowed(configuration, mapName, "CS2:AllowedMaps", "CS2_ALLOWED_MAPS", DefaultAllowedMaps, "map");
    }
}
