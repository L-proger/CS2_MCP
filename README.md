# CS2_MCP

This is my personal MCP server for running and administering private Counter-Strike 2 games. It is built around my local workflow and server preferences, not intended as a polished general-purpose CS2 hosting product.

C# MCP server for Counter-Strike 2 RCON, built with the official `ModelContextProtocol` C# SDK.
It supports stdio for local MCP clients and Streamable HTTP for HTTP MCP clients.

## Tools

CS2 RCON read-only tools:

- `cs2_status` - runs `status` and returns full raw output with server, map, player counts, and detailed player lines.
- `cs2_players` - runs `status` and returns a compact parsed player list.
- `cs2_server_info` - runs `status` and returns parsed server information plus raw status text.
- `cs2_map_info` - returns focused current map plus common map-related cvars.
- `cs2_get_cvar` - runs `help <name>` and extracts a cvar value when possible.
- `cs2_cvarlist` - runs `cvarlist` and returns a truncated list of commands and cvars.
- `cs2_list_gamemodes` - lists known gamemode presets accepted by `cs2_enable_gamemode`.
- `cs2_list_maps` - runs `maps <filter>` and returns a truncated local map list.
- `cs2_find` - runs `find <query>` to search commands and cvars.
- `cs2_help` - runs `help <name>` for a specific command or cvar.
- `cs2_ds_workshop_listmaps` - runs `ds_workshop_listmaps` and returns a truncated Workshop map list.

CS2 RCON action tools:

- `cs2_say` - sends a server chat message.
- `cs2_restart_game` - runs `mp_restartgame <delaySeconds>`.
- `cs2_warmup_start` - runs `mp_warmup_start`.
- `cs2_warmup_end` - runs `mp_warmup_end`.
- `cs2_pause_match` - runs `mp_pause_match`.
- `cs2_unpause_match` - runs `mp_unpause_match`.
- `cs2_enable_overtime` - enables overtime and configures max rounds, start money, and overtime limit.
- `cs2_disable_overtime` - disables overtime.
- `cs2_enable_friendly_fire` - enables friendly fire.
- `cs2_disable_friendly_fire` - disables friendly fire.
- `cs2_set_freezetime` - sets round freeze time in seconds.
- `cs2_set_buytime` - sets buy time in seconds.
- `cs2_set_startmoney` - sets starting money.
- `cs2_set_roundtime` - sets round time in minutes.
- `cs2_set_maxrounds` - sets maximum rounds.
- `cs2_enable_gamemode` - runs an allowlisted `exec gamemode_<mode>` preset.
- `cs2_bot_add` - runs `bot_add`, `bot_add_ct`, or `bot_add_t`.
- `cs2_bot_kick` - runs `bot_kick`, optionally for a specific bot name.
- `cs2_bot_quota` - runs `bot_quota <quota>`.
- `cs2_bot_difficulty` - runs `bot_difficulty <difficulty>`.
- `cs2_change_map` - runs `changelevel <mapName>` for an allowed map.
- `cs2_changelevel_next_allowed_map` - changes to the next map from the allowed map list.
- `cs2_host_workshop_map` - runs `host_workshop_map <workshopId>` for an allowed Workshop file id.
- `cs2_ds_workshop_changelevel` - runs `ds_workshop_changelevel <workshopId>` for an allowed Workshop file id.
- `cs2_kick_userid` - runs `kickid <userid> [reason]`.
- `cs2_set_cvar` - sets an explicitly allowlisted cvar.
- `cs2_exec_config` - executes an explicitly allowlisted config.

Dangerous tools are marked with MCP annotations such as `ReadOnly = false` and `Destructive = true`.

Known gamemode keys for `cs2_enable_gamemode`:

```text
armsrace
casual
competitive
competitive2v2
competitive2v2_offline
competitive_offline
competitive_short
competitive_tmm
cooperative
coopmission
custom
deathmatch
deathmatch_short
deathmatch_tmm
demolition
dm_freeforall
new_user_training
retakecasual
teamdeathmatch
workshop
```

The tool also accepts full config-like names such as `gamemode_competitive_offline.cfg`, but still executes only entries from this built-in list.

## RCON configuration

### Set the CS2 server password

On the CS2 dedicated server, set the RCON password in `game\csgo\cfg\server.cfg`:

```cfg
rcon_password "your-rcon-password"
```

You can also pass it as a server launch option:

```text
+rcon_password "your-rcon-password"
```

Using `server.cfg` is usually more convenient because it lives with the rest of the server configuration. This is not the same as `sv_password`: `sv_password` controls who can join the server, while `rcon_password` controls remote console access.

RCON uses TCP on the server port, usually `27015`. If this MCP server runs on another machine, make sure the CS2 server firewall allows that connection.

### Configure this MCP server

Set these values before using CS2 RCON tools:

```powershell
$env:CS2_RCON_HOST = "127.0.0.1"
$env:CS2_RCON_PORT = "27015"
$env:CS2_RCON_PASSWORD = "your-rcon-password"
```

`CS2_RCON_HOST` defaults to `127.0.0.1` and `CS2_RCON_PORT` defaults to `27015`; the password is required.

## VS Code Debug Configuration

The VS Code debug profiles load local secrets from `.env` through `envFile`. The `.env` file is ignored by git; commit `.env.example`, not `.env`.

```powershell
Copy-Item .env.example .env
```

Then edit `.env` and set:

```text
CS2_RCON_PASSWORD=your-real-rcon-password
```

Optional allowlists:

```powershell
$env:CS2_ALLOWED_MAPS = "de_dust2,de_mirage,de_nuke"
$env:CS2_ALLOWED_WORKSHOP_IDS = "3070244462,3070923343"
$env:CS2_ALLOWED_CVARS = "mp_roundtime,mp_freezetime,mp_maxrounds"
$env:CS2_ALLOWED_CONFIGS = "practice.cfg,server.cfg"
$env:CS2_BYPASS_ALLOWLISTS = "false"
```

If `CS2_ALLOWED_MAPS` is not set, a built-in allowlist of common official maps is used. `CS2_ALLOWED_WORKSHOP_IDS`, `CS2_ALLOWED_CVARS`, and `CS2_ALLOWED_CONFIGS` default to empty, so `cs2_host_workshop_map`, `cs2_ds_workshop_changelevel`, `cs2_set_cvar`, and `cs2_exec_config` require explicit configuration.

Set `CS2_BYPASS_ALLOWLISTS=true` to skip these allowlist checks. The equivalent configuration key is `CS2:BypassAllowlists=true`. This allows:

- any validated map name in `cs2_change_map`
- any validated Workshop file id in `cs2_host_workshop_map` and `cs2_ds_workshop_changelevel`
- any validated cvar name/value in `cs2_set_cvar`
- any validated config name in `cs2_exec_config`

The bypass does not disable argument validation; semicolons, quotes, newlines, and unsafe characters are still rejected where relevant.

## Run over stdio

```powershell
dotnet run --project .
```

MCP communicates through stdin/stdout using the SDK stdio transport. A local web UI is also started by default:

```text
http://127.0.0.1:3001/ui
```

Override the stdio web UI URL when needed:

```powershell
dotnet run --project . -- --web-ui-url http://127.0.0.1:3012
```

## Run over HTTP

```powershell
dotnet run --project . -- --transport http
```

Default URL:

```text
http://127.0.0.1:3001/mcp
```

Web UI:

```text
http://127.0.0.1:3001/ui
```

Override the URL when needed:

```powershell
dotnet run --project . -- --transport http --urls http://127.0.0.1:3017
```

Then use:

```text
http://127.0.0.1:3017/mcp
http://127.0.0.1:3017/ui
```

## Build self-contained app

Use the helper script to publish a self-contained app that does not require .NET to be installed on the target machine:

```powershell
.\build-self-contained.ps1
```

Default output:

```text
artifacts/self-contained/win-x64/
```

The default build is `Release`, `win-x64`, self-contained, single-file, and non-trimmed.

Useful options:

```powershell
.\build-self-contained.ps1 -Runtime linux-x64
.\build-self-contained.ps1 -Runtime win-x64 -Clean
.\build-self-contained.ps1 -Runtime win-x64 -NoSingleFile
```

There is also a `cmd` wrapper for shells that cannot run PowerShell scripts directly:

```cmd
build-self-contained.cmd -Runtime win-x64 -Clean
```

Run stdio from the publish output:

```powershell
.\artifacts\self-contained\win-x64\CS2_MCP.exe
```

Run HTTP from the publish output:

```powershell
.\artifacts\self-contained\win-x64\CS2_MCP.exe --transport http
```

## Web UI

The web UI is a small button-based RCON control page backed by the same `Cs2RconClient` as the MCP tools. It includes status refresh, server chat, quick map changes from `CS2_ALLOWED_MAPS`, gamemode competitive offline, game restart, warmup, pause/unpause, overtime, friendly fire, match rule setters, and bot controls.

In stdio mode, the web UI runs on a local ASP.NET server at `/ui`, while MCP uses stdin/stdout and no `/mcp` HTTP endpoint is exposed. In HTTP mode, the web UI is served by the same ASP.NET app as the MCP HTTP endpoint. The default port is `3001` in both modes.

If several stdio instances are started by the MCP client, only one process can own the web UI port. Other instances keep retrying in the background and can take over the web UI if the current owner exits.

Use `127.0.0.1` for the local web UI instead of `localhost`. On Windows, `localhost` may resolve to both IPv4 and IPv6 loopback addresses, which can make two short-lived stdio processes appear to start a web UI on the same port.

Diagnostics are written to:

```text
logs/cs2-mcp.log
```

With `dotnet run`, this is under the build output directory, for example `bin/Debug/net8.0/logs/cs2-mcp.log`.

### CFG scripts

The web UI also shows buttons for local CS2 `.cfg` script files and includes a small script editor. When running from source, scripts are loaded from the project `scripts` directory:

```text
scripts/
```

In a published app or Docker image, the default is the `scripts` directory next to the application binaries.

Supported extension:

```text
.cfg
```

Each non-empty line is executed as one RCON command. Lines starting with `#` or `//` are ignored.

Example script:

```text
# scripts/live-overtime.cfg
exec gamemode_competitive_offline
mp_overtime_enable 1
mp_overtime_maxrounds 6
mp_overtime_startmoney 10000
mp_restartgame 1
```

The button name is the script file name without extension, for example `live-overtime`.

From the web UI you can:

- refresh the script list
- run a script by pressing its script-name button
- load a script into the editor with `Edit`
- create a new script by typing a file name and pressing `Save Script`
- edit an existing script and save it back

If the file name has no extension, `.cfg` is added automatically.

Files from the project `scripts` directory are copied to build and publish output, so adding scripts there works naturally with `dotnet run`, `dotnet build`, and `dotnet publish`.

Override the scripts directory when needed:

```powershell
$env:CS2_SCRIPTS_DIR = "C:\path\to\scripts"
```

## Run with Docker

Build the image:

```powershell
docker build -t cs2-mcp .
```

Run over stdio with `docker run -e` values:

```powershell
docker run --rm -i `
  -e CS2_RCON_HOST=host.docker.internal `
  -e CS2_RCON_PORT=27015 `
  -e CS2_RCON_PASSWORD=your-rcon-password `
  -e CS2_ALLOWED_MAPS=de_dust2,de_mirage,de_nuke `
  cs2-mcp
```

Or create a local `.env` from `.env.example` and pass it to Docker:

```powershell
Copy-Item .env.example .env
docker run --rm -i --env-file .env cs2-mcp
```

The default container command uses stdio, same as running the project without parameters. Keep `-i` enabled for stdio MCP clients so Docker keeps stdin open. To expose the stdio web UI from Docker, publish port `3001`:

```powershell
docker run --rm -i `
  --env-file .env `
  -p 3001:3001 `
  cs2-mcp
```

If your `.env` explicitly sets `CS2_WEB_UI_URL`, use `CS2_WEB_UI_URL=http://0.0.0.0:3001` inside Docker.

Mount local scripts into the default app-side `scripts` directory:

```powershell
docker run --rm -i `
  --env-file .env `
  -p 3001:3001 `
  -v ${PWD}\scripts:/app/scripts `
  cs2-mcp
```

Run over HTTP from Docker:

```powershell
docker run --rm `
  --env-file .env `
  -p 3001:3001 `
  cs2-mcp --transport http
```

HTTP endpoint:

```text
http://127.0.0.1:3001/mcp
http://127.0.0.1:3001/ui
```

If the CS2 server runs on the same Windows or macOS machine as Docker Desktop, use `CS2_RCON_HOST=host.docker.internal` instead of `127.0.0.1`. On Linux, use the server's reachable IP address or run the container with host networking when appropriate.

## Recommended system prompt

Use a system prompt like this for a local LLM client connected to this MCP server:

```text
You are an assistant helping administer a private Counter-Strike 2 server through MCP tools.

Use CS2 MCP tools carefully and prefer specific tools over generic cvar/config tools.

Before changing server state, inspect the current state when relevant:
- use cs2_status or cs2_server_info for server/map/player state
- use cs2_players before kicking a player
- use cs2_list_gamemodes before changing gamemode if the requested mode is ambiguous

When the user asks for all player info, full player details, or all server/player details, prefer cs2_status or cs2_server_info. cs2_players is only a compact parsed player list.

Do not invent map names, workshop ids, player userids, cvar names, or config names.
Use discovery tools first:
- cs2_list_maps for local maps
- cs2_ds_workshop_listmaps for Workshop maps
- cs2_find, cs2_help, cs2_get_cvar for commands/cvars
- cs2_list_gamemodes for gamemode presets

For destructive or disruptive actions, briefly state what you are about to do and ask for confirmation unless the user gave a direct explicit command in the current message.
Disruptive actions include:
- changing map or Workshop map
- restarting the game
- kicking players
- changing gamemode
- executing configs
- changing match-critical rules during a live game

When setting match rules, use the dedicated tools when available:
- cs2_enable_gamemode
- cs2_restart_game
- cs2_enable_overtime / cs2_disable_overtime
- cs2_set_maxrounds
- cs2_set_freezetime
- cs2_set_buytime
- cs2_set_startmoney
- cs2_set_roundtime
- cs2_enable_friendly_fire / cs2_disable_friendly_fire

For the user's preferred 5v5 with bots replaced by humans, use:
- cs2_enable_gamemode with mode competitive_offline

Do not use cs2_set_cvar or cs2_exec_config when a dedicated tool exists.
Do not use bypassed allowlists as permission to run arbitrary risky changes.
If a tool call fails or returns unclear output, explain the result and suggest the safest next check.
Keep responses concise and operational.
```

## LM Studio example

For stdio MCP clients such as LM Studio, build first:

```powershell
dotnet build
```

Then point LM Studio to the project with `dotnet run --no-build`. This still runs the project, but avoids build-time CLI output in the stdio MCP channel:

```json
{
  "mcpServers": {
    "cs2-mcp": {
      "command": "dotnet",
      "args": [
        "run",
        "--no-build",
        "--project",
        "C:\\path\\to\\CS2_MCP\\CS2_MCP.csproj"
      ],
      "env": {
        "DOTNET_NOLOGO": "true",
        "CS2_RCON_HOST": "127.0.0.1",
        "CS2_RCON_PORT": "27015",
        "CS2_RCON_PASSWORD": "your-rcon-password",
        "CS2_ALLOWED_MAPS": "de_dust2,de_mirage,de_nuke",
        "CS2_ALLOWED_WORKSHOP_IDS": "3070244462,3070923343",
        "CS2_ALLOWED_CVARS": "mp_roundtime,mp_freezetime,mp_maxrounds",
        "CS2_ALLOWED_CONFIGS": "practice.cfg,server.cfg",
        "CS2_BYPASS_ALLOWLISTS": "false"
      }
    }
  }
}
```

This keeps using stdio. Do not add `--transport http` for LM Studio's local MCP entry unless the client is configured for MCP over HTTP.

Alternatively, publish a self-contained executable with `build-self-contained.ps1` and use the produced executable directly.
