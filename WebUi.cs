using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol;

internal static class WebUi
{
    internal const string Path = "/ui";
    private const string ApiPath = "/api/ui";

    public static void Map(WebApplication app, string? mcpPath)
    {
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments(ApiPath))
            {
                DiagnosticsLog.Write($"Web UI request begin: {context.Request.Method} {context.Request.Path}");
                try
                {
                    await next();
                    DiagnosticsLog.Write($"Web UI request end: {context.Request.Method} {context.Request.Path} {context.Response.StatusCode}");
                }
                catch (Exception ex)
                {
                    DiagnosticsLog.Write($"Web UI request failed: {context.Request.Method} {context.Request.Path}", ex);
                    throw;
                }

                return;
            }

            await next();
        });

        app.MapGet("/", () => Results.Redirect(Path));
        app.MapGet(Path, () => Results.Content(BuildHtml(mcpPath), "text/html; charset=utf-8"));
        app.MapGet($"{ApiPath}/status", GetStatusAsync);
        app.MapGet($"{ApiPath}/maps", ListMaps);
        app.MapGet($"{ApiPath}/scripts", ListScripts);
        app.MapPost($"{ApiPath}/scripts/read", ReadScriptAsync);
        app.MapPost($"{ApiPath}/scripts/save", SaveScriptAsync);
        app.MapPost($"{ApiPath}/scripts/delete", DeleteScriptAsync);
        app.MapPost($"{ApiPath}/scripts/run", RunScriptAsync);
        app.MapPost($"{ApiPath}/actions/{{actionId}}", ExecuteActionAsync);
    }

    private static async Task<IResult> GetStatusAsync(Cs2RconClient rcon)
    {
        try
        {
            var status = await rcon.SendCommandAsync("status", 20000);
            return Results.Json(new { ok = true, result = status });
        }
        catch (Exception ex)
        {
            DiagnosticsLog.Write("Web UI status request failed.", ex);
            return Results.Json(new { ok = false, error = ex.Message }, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> ExecuteActionAsync(string actionId, HttpContext context, Cs2RconClient rcon, IConfiguration configuration)
    {
        WebUiActionRequest request;
        try
        {
            request = await context.Request.ReadFromJsonAsync<WebUiActionRequest>() ?? new WebUiActionRequest();
        }
        catch (JsonException)
        {
            request = new WebUiActionRequest();
        }

        try
        {
            var result = await ExecuteActionCoreAsync(actionId, request, rcon, configuration);
            return Results.Json(new { ok = true, actionId, result });
        }
        catch (Exception ex)
        {
            DiagnosticsLog.Write($"Web UI action '{actionId}' failed.", ex);
            return Results.Json(new { ok = false, actionId, error = ex.Message }, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static IResult ListScripts(Cs2ScriptService scripts)
    {
        try
        {
            return Results.Json(new { ok = true, result = scripts.ListScripts() });
        }
        catch (Exception ex)
        {
            DiagnosticsLog.Write("Web UI script list failed.", ex);
            return Results.Json(new { ok = false, error = ex.Message }, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static IResult ListMaps(IConfiguration configuration)
    {
        try
        {
            return Results.Json(new { ok = true, result = Tools.GetConfiguredAllowedMaps(configuration) });
        }
        catch (Exception ex)
        {
            DiagnosticsLog.Write("Web UI map list failed.", ex);
            return Results.Json(new { ok = false, error = ex.Message }, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> RunScriptAsync(HttpContext context, Cs2ScriptService scripts)
    {
        WebUiScriptRunRequest request;
        try
        {
            request = await context.Request.ReadFromJsonAsync<WebUiScriptRunRequest>() ?? new WebUiScriptRunRequest();
        }
        catch (JsonException)
        {
            request = new WebUiScriptRunRequest();
        }

        try
        {
            var result = await scripts.RunScriptAsync(request.ScriptFileName ?? string.Empty);
            return Results.Json(new { ok = true, result });
        }
        catch (Exception ex)
        {
            DiagnosticsLog.Write($"Web UI script run '{request.ScriptFileName}' failed.", ex);
            return Results.Json(new { ok = false, error = ex.Message }, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> ReadScriptAsync(HttpContext context, Cs2ScriptService scripts)
    {
        WebUiScriptRunRequest request;
        try
        {
            request = await context.Request.ReadFromJsonAsync<WebUiScriptRunRequest>() ?? new WebUiScriptRunRequest();
        }
        catch (JsonException)
        {
            request = new WebUiScriptRunRequest();
        }

        try
        {
            var result = scripts.ReadScript(request.ScriptFileName ?? string.Empty);
            return Results.Json(new { ok = true, result });
        }
        catch (Exception ex)
        {
            DiagnosticsLog.Write($"Web UI script read '{request.ScriptFileName}' failed.", ex);
            return Results.Json(new { ok = false, error = ex.Message }, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> SaveScriptAsync(HttpContext context, Cs2ScriptService scripts)
    {
        WebUiScriptSaveRequest request;
        try
        {
            request = await context.Request.ReadFromJsonAsync<WebUiScriptSaveRequest>() ?? new WebUiScriptSaveRequest();
        }
        catch (JsonException)
        {
            request = new WebUiScriptSaveRequest();
        }

        try
        {
            var result = scripts.SaveScript(request.ScriptFileName ?? string.Empty, request.Content ?? string.Empty);
            return Results.Json(new { ok = true, result });
        }
        catch (Exception ex)
        {
            DiagnosticsLog.Write($"Web UI script save '{request.ScriptFileName}' failed.", ex);
            return Results.Json(new { ok = false, error = ex.Message }, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> DeleteScriptAsync(HttpContext context, Cs2ScriptService scripts)
    {
        WebUiScriptRunRequest request;
        try
        {
            request = await context.Request.ReadFromJsonAsync<WebUiScriptRunRequest>() ?? new WebUiScriptRunRequest();
        }
        catch (JsonException)
        {
            request = new WebUiScriptRunRequest();
        }

        try
        {
            var result = scripts.DeleteScript(request.ScriptFileName ?? string.Empty);
            return Results.Json(new { ok = true, result });
        }
        catch (Exception ex)
        {
            DiagnosticsLog.Write($"Web UI script delete '{request.ScriptFileName}' failed.", ex);
            return Results.Json(new { ok = false, error = ex.Message }, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static Task<object> ExecuteActionCoreAsync(string actionId, WebUiActionRequest request, Cs2RconClient rcon, IConfiguration configuration)
    {
        return actionId switch
        {
            "say" => SendCommandAsObjectAsync(rcon, $"say {GetSafeMessage(request)}"),
            "competitive_offline" => SendCommandAsObjectAsync(rcon, "exec gamemode_competitive_offline"),
            "restart_game" => SendCommandAsObjectAsync(rcon, $"mp_restartgame {Clamp(request.DelaySeconds, 1, 0, 60)}"),
            "warmup_start" => SendCommandAsObjectAsync(rcon, "mp_warmup_start"),
            "warmup_end" => SendCommandAsObjectAsync(rcon, "mp_warmup_end"),
            "pause_match" => SendCommandAsObjectAsync(rcon, "mp_pause_match"),
            "unpause_match" => SendCommandAsObjectAsync(rcon, "mp_unpause_match"),
            "overtime_on" => SendCommandsAsObjectAsync(
                rcon,
                [
                    "mp_overtime_enable 1",
                    $"mp_overtime_maxrounds {Clamp(request.MaxRounds, 6, 2, 30)}",
                    $"mp_overtime_startmoney {Clamp(request.StartMoney, 10000, 800, 65535)}",
                    $"mp_overtime_limit {Clamp(request.Limit, 0, 0, 30)}"
                ]),
            "overtime_off" => SendCommandAsObjectAsync(rcon, "mp_overtime_enable 0"),
            "friendly_fire_on" => SendCommandAsObjectAsync(rcon, "mp_friendlyfire 1"),
            "friendly_fire_off" => SendCommandAsObjectAsync(rcon, "mp_friendlyfire 0"),
            "set_freezetime" => SendCommandAsObjectAsync(rcon, $"mp_freezetime {Clamp(request.Seconds, 15, 0, 60)}"),
            "set_buytime" => SendCommandAsObjectAsync(rcon, $"mp_buytime {Clamp(request.Seconds, 20, 0, 600)}"),
            "set_startmoney" => SendCommandAsObjectAsync(rcon, $"mp_startmoney {Clamp(request.Money, 800, 800, 65535)}"),
            "set_maxrounds" => SendCommandAsObjectAsync(rcon, $"mp_maxrounds {Clamp(request.Rounds, 24, 1, 60)}"),
            "set_roundtime" => SetRoundTimeAsync(rcon, Clamp(request.Minutes, 2, 1, 60)),
            "bot_quota" => SendCommandAsObjectAsync(rcon, $"bot_quota {Clamp(request.Quota, 10, 0, 64)}"),
            "bot_difficulty" => SendCommandAsObjectAsync(rcon, $"bot_difficulty {Clamp(request.Difficulty, 2, 0, 3)}"),
            "bot_kick" => SendCommandAsObjectAsync(rcon, "bot_kick"),
            "change_map" => ChangeMapAsync(rcon, configuration, request.MapName ?? string.Empty),
            _ => throw new McpException($"Unknown web UI action '{actionId}'.")
        };
    }

    private static async Task<object> ChangeMapAsync(Cs2RconClient rcon, IConfiguration configuration, string mapName)
    {
        Tools.ValidateAllowedMap(configuration, mapName);
        return await rcon.SendCommandAsync($"changelevel {mapName}", 12000);
    }

    private static async Task<object> SetRoundTimeAsync(Cs2RconClient rcon, int minutes)
    {
        return await rcon.SendCommandsAsync(
            [
                $"mp_roundtime {minutes}",
                $"mp_roundtime_defuse {minutes}",
                $"mp_roundtime_hostage {minutes}"
            ],
            4000);
    }

    private static async Task<object> SendCommandAsObjectAsync(Cs2RconClient rcon, string command)
    {
        return await rcon.SendCommandAsync(command, 12000);
    }

    private static async Task<object> SendCommandsAsObjectAsync(Cs2RconClient rcon, IEnumerable<string> commands)
    {
        return await rcon.SendCommandsAsync(commands, 4000);
    }

    private static int Clamp(int? value, int fallback, int min, int max)
    {
        return Math.Clamp(value ?? fallback, min, max);
    }

    private static string GetSafeMessage(WebUiActionRequest request)
    {
        var message = request.Message?.Trim();
        if (string.IsNullOrWhiteSpace(message) ||
            message.Length > 200 ||
            message.Contains(';') ||
            message.Contains('"') ||
            message.Contains('\r') ||
            message.Contains('\n'))
        {
            throw new McpException("Message must be 1-200 characters and cannot contain semicolons, quotes, or newlines.");
        }

        return message;
    }

    private static string BuildHtml(string? mcpPath)
    {
        var mcpLine = string.IsNullOrWhiteSpace(mcpPath)
            ? "MCP transport: stdio"
            : $"MCP endpoint: {mcpPath}";

        return $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>CS2 MCP Control</title>
  <style>
    :root {
      color-scheme: dark;
      --bg: #111316;
      --panel: #1b1f24;
      --panel-2: #222831;
      --text: #edf0f2;
      --muted: #aab2bc;
      --line: #343b44;
      --accent: #56b6c2;
      --danger: #ef6f6c;
      --ok: #8ccf7e;
      --input: #15191e;
    }

    * { box-sizing: border-box; }

    body {
      margin: 0;
      background: var(--bg);
      color: var(--text);
      font-family: Segoe UI, system-ui, sans-serif;
      font-size: 14px;
    }

    header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 16px;
      padding: 16px 20px;
      border-bottom: 1px solid var(--line);
      background: #161a1f;
    }

    h1 {
      margin: 0;
      font-size: 18px;
      font-weight: 650;
    }

    .subtle {
      color: var(--muted);
      font-size: 12px;
      white-space: nowrap;
    }

    main {
      display: grid;
      grid-template-columns: minmax(320px, 520px) minmax(360px, 1fr);
      gap: 16px;
      padding: 16px;
      max-width: 1400px;
      margin: 0 auto;
    }

    section {
      border: 1px solid var(--line);
      background: var(--panel);
      border-radius: 8px;
      overflow: hidden;
    }

    section h2 {
      margin: 0;
      padding: 11px 12px;
      font-size: 13px;
      font-weight: 650;
      color: var(--muted);
      border-bottom: 1px solid var(--line);
      background: var(--panel-2);
    }

    .stack {
      display: grid;
      gap: 12px;
    }

    .body {
      padding: 12px;
    }

    .grid {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 8px;
    }

    .map-grid {
      display: grid;
      grid-template-columns: repeat(3, minmax(0, 1fr));
      gap: 6px;
    }

    .map-grid button {
      min-height: 30px;
      padding: 5px 7px;
      overflow: hidden;
      white-space: nowrap;
      text-overflow: ellipsis;
    }

    .script-list {
      display: grid;
      gap: 6px;
    }

    .script-row {
      display: grid;
      grid-template-columns: minmax(0, 1fr) 32px 32px;
      gap: 6px;
      align-items: center;
    }

    .script-run {
      min-height: 30px;
      padding: 5px 8px;
      text-align: left;
      overflow: hidden;
      white-space: nowrap;
      text-overflow: ellipsis;
    }

    .script-edit {
      display: grid;
      place-items: center;
      width: 32px;
      min-height: 30px;
      padding: 0;
      font-size: 14px;
    }

    .script-delete {
      border-color: #6b3534;
      color: #ffd7d6;
    }

    .script-editor {
      display: grid;
      gap: 8px;
    }

    .script-editor[hidden] {
      display: none;
    }

    .row {
      display: grid;
      grid-template-columns: 1fr auto;
      gap: 8px;
      align-items: center;
    }

    .row.three {
      grid-template-columns: 1fr 92px auto;
    }

    button {
      min-height: 34px;
      border: 1px solid var(--line);
      border-radius: 6px;
      background: #2b333d;
      color: var(--text);
      font: inherit;
      cursor: pointer;
      padding: 7px 10px;
    }

    button:hover { border-color: var(--accent); }
    button:disabled { opacity: .55; cursor: progress; }
    button.danger { border-color: #6b3534; color: #ffd7d6; }
    button.good { border-color: #3b5f42; color: #dcf8d8; }
    button.drag-source { cursor: grab; }
    button.drag-source:active { cursor: grabbing; }

    input {
      min-height: 34px;
      min-width: 0;
      border: 1px solid var(--line);
      border-radius: 6px;
      background: var(--input);
      color: var(--text);
      font: inherit;
      padding: 7px 9px;
    }

    input[type="number"] {
      width: 92px;
    }

    textarea {
      min-height: 170px;
      width: 100%;
      resize: vertical;
      border: 1px solid var(--line);
      border-radius: 6px;
      background: var(--input);
      color: var(--text);
      font: 12px/1.45 Consolas, ui-monospace, monospace;
      padding: 9px;
    }

    textarea.drag-over {
      border-color: var(--accent);
      box-shadow: 0 0 0 2px rgba(86, 182, 194, .22);
    }

    pre {
      min-height: 520px;
      max-height: calc(100vh - 150px);
      overflow: auto;
      margin: 0;
      padding: 12px;
      background: #0d1013;
      color: #d8dee9;
      border-top: 1px solid var(--line);
      font: 12px/1.45 Consolas, ui-monospace, monospace;
      white-space: pre-wrap;
    }

    .statusline {
      min-height: 28px;
      padding: 7px 12px;
      border-top: 1px solid var(--line);
      color: var(--muted);
      font-size: 12px;
    }

    .statusline.ok { color: var(--ok); }
    .statusline.error { color: var(--danger); }

    @media (max-width: 900px) {
      header { align-items: flex-start; flex-direction: column; }
      .subtle { white-space: normal; }
      main { grid-template-columns: 1fr; }
      pre { min-height: 360px; max-height: 560px; }
    }
  </style>
</head>
<body>
  <header>
    <h1>CS2 Server Control</h1>
    <div class="subtle">{{mcpLine}} - Web UI API: /api/ui</div>
  </header>

  <main>
    <div class="stack">
      <section>
        <h2>Server</h2>
        <div class="body stack">
          <div class="grid">
            <button class="good" data-status>Refresh Status</button>
            <button data-action="competitive_offline" data-confirm="Switch gamemode to competitive_offline?">Competitive Offline</button>
            <button class="danger" data-action="restart_game" data-payload="delaySeconds:#restartDelay" data-confirm="Restart the game?">Restart Game</button>
            <button data-action="warmup_start">Warmup Start</button>
            <button data-action="warmup_end">Warmup End</button>
            <button data-action="pause_match">Pause Match</button>
            <button data-action="unpause_match">Unpause Match</button>
            <button data-action="bot_kick" data-confirm="Kick all bots?">Kick Bots</button>
          </div>
          <div class="row">
            <input id="sayMessage" placeholder="Server chat message" maxlength="200">
            <button data-action="say" data-payload="message:#sayMessage">Say</button>
          </div>
          <div class="row three">
            <span class="subtle">Restart delay</span>
            <input id="restartDelay" type="number" value="1" min="0" max="60">
            <span class="subtle">seconds</span>
          </div>
        </div>
      </section>

      <section>
        <h2>Scripts</h2>
        <div class="body stack">
          <div class="row">
            <span class="subtle">Files from the scripts directory</span>
            <button data-refresh-scripts>Refresh Scripts</button>
          </div>
          <div id="scriptList" class="script-list">
            <button disabled>No scripts loaded</button>
          </div>
          <div class="row">
            <span class="subtle">Script editor</span>
            <button id="scriptEditorToggle" data-toggle-script-editor>Show Editor</button>
          </div>
          <div id="scriptEditor" class="script-editor" hidden>
            <div class="row">
              <input id="scriptFileName" placeholder="script-name">
              <button data-new-script>New</button>
            </div>
            <textarea id="scriptContent" spellcheck="false" placeholder="One RCON command per line"></textarea>
            <div class="row">
              <span id="scriptMeta" class="subtle">No script selected</span>
              <button data-save-script>Save Script</button>
            </div>
          </div>
        </div>
      </section>

      <section>
        <h2>Maps</h2>
        <div class="body stack">
          <div class="row">
            <span class="subtle">Allowed maps</span>
            <button data-refresh-maps>Refresh Maps</button>
          </div>
          <div id="mapList" class="map-grid">
            <button disabled>No maps loaded</button>
          </div>
        </div>
      </section>

      <section>
        <h2>Match Rules</h2>
        <div class="body stack">
          <div class="grid">
            <button data-action="overtime_on">Overtime On</button>
            <button data-action="overtime_off">Overtime Off</button>
            <button data-action="friendly_fire_on">Friendly Fire On</button>
            <button data-action="friendly_fire_off">Friendly Fire Off</button>
          </div>
          <div class="row three">
            <span>Freeze time</span>
            <input id="freezeSeconds" type="number" value="15" min="0" max="60">
            <button data-action="set_freezetime" data-payload="seconds:#freezeSeconds" data-script-label="Set freeze time">Set</button>
          </div>
          <div class="row three">
            <span>Buy time</span>
            <input id="buySeconds" type="number" value="20" min="0" max="600">
            <button data-action="set_buytime" data-payload="seconds:#buySeconds" data-script-label="Set buy time">Set</button>
          </div>
          <div class="row three">
            <span>Start money</span>
            <input id="startMoney" type="number" value="800" min="800" max="65535">
            <button data-action="set_startmoney" data-payload="money:#startMoney" data-script-label="Set start money">Set</button>
          </div>
          <div class="row three">
            <span>Round time</span>
            <input id="roundMinutes" type="number" value="2" min="1" max="60">
            <button data-action="set_roundtime" data-payload="minutes:#roundMinutes" data-script-label="Set round time">Set</button>
          </div>
          <div class="row three">
            <span>Max rounds</span>
            <input id="maxRounds" type="number" value="24" min="1" max="60">
            <button data-action="set_maxrounds" data-payload="rounds:#maxRounds" data-script-label="Set max rounds">Set</button>
          </div>
        </div>
      </section>

      <section>
        <h2>Bots</h2>
        <div class="body stack">
          <div class="row three">
            <span>Bot quota</span>
            <input id="botQuota" type="number" value="10" min="0" max="64">
            <button data-action="bot_quota" data-payload="quota:#botQuota" data-script-label="Set bot quota">Set</button>
          </div>
          <div class="row three">
            <span>Difficulty</span>
            <input id="botDifficulty" type="number" value="2" min="0" max="3">
            <button data-action="bot_difficulty" data-payload="difficulty:#botDifficulty" data-script-label="Set bot difficulty">Set</button>
          </div>
        </div>
      </section>
    </div>

    <section>
      <h2>Output</h2>
      <pre id="output">Ready.</pre>
      <div id="statusLine" class="statusline">Idle</div>
    </section>
  </main>

  <script>
    const output = document.querySelector('#output');
    const statusLine = document.querySelector('#statusLine');
    const mapList = document.querySelector('#mapList');
    const scriptList = document.querySelector('#scriptList');
    const scriptEditor = document.querySelector('#scriptEditor');
    const scriptEditorToggle = document.querySelector('#scriptEditorToggle');
    const scriptFileName = document.querySelector('#scriptFileName');
    const scriptContent = document.querySelector('#scriptContent');
    const scriptMeta = document.querySelector('#scriptMeta');

    document.querySelectorAll('[data-status]').forEach(button => {
      button.addEventListener('click', () => runStatus(button));
    });

    document.querySelectorAll('[data-refresh-scripts]').forEach(button => {
      button.addEventListener('click', () => loadScripts(button));
    });

    document.querySelectorAll('[data-refresh-maps]').forEach(button => {
      button.addEventListener('click', () => loadMaps(button));
    });

    document.querySelectorAll('[data-new-script]').forEach(button => {
      button.addEventListener('click', () => newScript());
    });

    document.querySelectorAll('[data-toggle-script-editor]').forEach(button => {
      button.addEventListener('click', () => setScriptEditorVisible(scriptEditor.hidden));
    });

    document.querySelectorAll('[data-save-script]').forEach(button => {
      button.addEventListener('click', () => saveScript(button));
    });

    document.querySelectorAll('[data-action]').forEach(button => setupActionButton(button));
    setupScriptDropTarget();

    loadScripts();
    loadMaps();

    async function runStatus(button) {
      await withBusy(button, async () => {
        const response = await fetch('/api/ui/status');
        const payload = await readResponsePayload(response);
        showPayload(payload);
      });
    }

    async function runAction(button) {
      const confirmText = button.dataset.confirm;
      if (confirmText && !confirm(confirmText)) {
        return;
      }

      await withBusy(button, async () => {
        const response = await fetch(`/api/ui/actions/${button.dataset.action}`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(readPayload(button.dataset.payload))
        });
        const payload = await readResponsePayload(response);
        showPayload(payload);
      });
    }

    async function loadScripts(button) {
      const work = async () => {
        const response = await fetch('/api/ui/scripts');
        const payload = await readResponsePayload(response);

        if (!payload.ok) {
          showPayload(payload);
          return;
        }

        renderScripts(payload.result);
      };

      if (button) {
        await withBusy(button, work);
      } else {
        await work();
      }
    }

    async function loadMaps(button) {
      const work = async () => {
        const response = await fetch('/api/ui/maps');
        const payload = await readResponsePayload(response);

        if (!payload.ok) {
          showPayload(payload);
          return;
        }

        renderMaps(payload.result);
      };

      if (button) {
        await withBusy(button, work);
      } else {
        await work();
      }
    }

    function renderMaps(maps) {
      mapList.replaceChildren();

      if (!maps || maps.length === 0) {
        const empty = document.createElement('button');
        empty.disabled = true;
        empty.textContent = 'No maps found';
        mapList.appendChild(empty);
        return;
      }

      for (const mapName of maps) {
        const button = document.createElement('button');
        button.textContent = mapName;
        button.title = `Change map to ${mapName}`;
        button.dataset.action = 'change_map';
        button.dataset.mapName = mapName;
        button.dataset.scriptLabel = `Change map to ${mapName}`;
        makeActionButtonDraggable(button);
        button.addEventListener('click', () => changeMap(button, mapName));
        mapList.appendChild(button);
      }
    }

    async function changeMap(button, mapName) {
      if (!confirm(`Change map to "${mapName}"?`)) {
        return;
      }

      await withBusy(button, async () => {
        const response = await fetch('/api/ui/actions/change_map', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ mapName })
        });
        const payload = await readResponsePayload(response);
        showPayload(payload);
      });
    }

    function renderScripts(scripts) {
      scriptList.replaceChildren();

      if (!scripts || scripts.length === 0) {
        const empty = document.createElement('button');
        empty.disabled = true;
        empty.textContent = 'No scripts found';
        scriptList.appendChild(empty);
        return;
      }

      for (const script of scripts) {
        const row = document.createElement('div');
        row.className = 'script-row';

        const runButton = document.createElement('button');
        runButton.className = 'script-run';
        runButton.textContent = script.name;
        runButton.title = `Run ${script.fileName} - ${script.commandCount} commands`;
        runButton.addEventListener('click', () => runScript(runButton, script));
        row.appendChild(runButton);

        const editButton = document.createElement('button');
        editButton.className = 'script-edit';
        editButton.innerHTML = '&#9998;';
        editButton.setAttribute('aria-label', `Edit ${script.fileName}`);
        editButton.title = `Edit ${script.fileName}`;
        editButton.addEventListener('click', () => readScript(editButton, script.fileName));
        row.appendChild(editButton);

        const deleteButton = document.createElement('button');
        deleteButton.className = 'script-edit script-delete';
        deleteButton.innerHTML = '&times;';
        deleteButton.setAttribute('aria-label', `Delete ${script.fileName}`);
        deleteButton.title = `Delete ${script.fileName}`;
        deleteButton.addEventListener('click', () => deleteScript(deleteButton, script));
        row.appendChild(deleteButton);

        scriptList.appendChild(row);
      }
    }

    async function runScript(button, script) {
      if (!confirm(`Run script "${script.name}"?\n\nFile: ${script.fileName}\nCommands: ${script.commandCount}`)) {
        return;
      }

      await withBusy(button, async () => {
        const response = await fetch('/api/ui/scripts/run', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ scriptFileName: script.fileName })
        });
        const payload = await readResponsePayload(response);
        showPayload(payload);
      });
    }

    function newScript() {
      setScriptEditorVisible(true);
      scriptFileName.value = 'new-script';
      scriptContent.value = '';
      scriptMeta.textContent = 'New script';
      scriptContent.focus();
    }

    async function readScript(button, fileName) {
      await withBusy(button, async () => {
        const response = await fetch('/api/ui/scripts/read', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ scriptFileName: fileName })
        });
        const payload = await readResponsePayload(response);

        if (!payload.ok) {
          showPayload(payload);
          return;
        }

        setScriptEditorVisible(true);
        scriptFileName.value = payload.result.name;
        scriptContent.value = payload.result.content;
        scriptMeta.textContent = `${payload.result.fileName} - ${payload.result.commandCount} commands`;
        showPayload(payload);
      });
    }

    async function deleteScript(button, script) {
      if (!confirm(`Delete script "${script.name}"?\n\nFile: ${script.fileName}`)) {
        return;
      }

      await withBusy(button, async () => {
        const response = await fetch('/api/ui/scripts/delete', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ scriptFileName: script.fileName })
        });
        const payload = await readResponsePayload(response);

        if (payload.ok) {
          if (scriptFileName.value === script.name || scriptFileName.value === script.fileName) {
            scriptFileName.value = '';
            scriptContent.value = '';
            scriptMeta.textContent = 'No script selected';
          }

          await loadScripts();
        }

        showPayload(payload);
      });
    }

    function setScriptEditorVisible(visible) {
      scriptEditor.hidden = !visible;
      scriptEditorToggle.textContent = visible ? 'Hide Editor' : 'Show Editor';
    }

    function setupActionButton(button) {
      button.addEventListener('click', () => runAction(button));
      makeActionButtonDraggable(button);
    }

    function makeActionButtonDraggable(button) {
      button.draggable = true;
      button.classList.add('drag-source');
      button.title = button.title
        ? `${button.title} - drag into script editor`
        : 'Drag into script editor';

      button.addEventListener('dragstart', event => {
        const commands = getScriptCommandsForAction(button);
        if (!commands.length) {
          event.preventDefault();
          return;
        }

        setScriptEditorVisible(true);
        event.dataTransfer.effectAllowed = 'copy';
        event.dataTransfer.setData('text/plain', buildScriptBlock(button, commands));
      });
    }

    function setupScriptDropTarget() {
      scriptContent.addEventListener('dragover', event => {
        if (!Array.from(event.dataTransfer.types).includes('text/plain')) {
          return;
        }

        event.preventDefault();
        event.dataTransfer.dropEffect = 'copy';
        scriptContent.classList.add('drag-over');
      });

      scriptContent.addEventListener('dragleave', () => {
        scriptContent.classList.remove('drag-over');
      });

      scriptContent.addEventListener('drop', event => {
        event.preventDefault();
        scriptContent.classList.remove('drag-over');
        appendCommandsToScript(event.dataTransfer.getData('text/plain'));
      });
    }

    function appendCommandsToScript(text) {
      const lines = text
        .split(/\r?\n/)
        .map(line => line.trim())
        .filter(line => line.length > 0);

      if (!lines.length) {
        return;
      }

      setScriptEditorVisible(true);
      const prefix = scriptContent.value.trimEnd();
      scriptContent.value = prefix
        ? `${prefix}\n${lines.join('\n')}\n`
        : `${lines.join('\n')}\n`;
      scriptMeta.textContent = 'Unsaved script changes';
      scriptContent.focus();
      scriptContent.selectionStart = scriptContent.value.length;
      scriptContent.selectionEnd = scriptContent.value.length;
      const executableCount = lines.filter(isExecutableScriptLine).length;
      setStatus(`Added ${executableCount} command(s) to script editor`, 'ok');
    }

    function buildScriptBlock(button, commands) {
      return [`// ${getScriptBlockLabel(button)}`, ...commands].join('\n');
    }

    function getScriptBlockLabel(button) {
      const label = (button.dataset.scriptLabel || button.textContent || button.dataset.action || 'Action')
        .replace(/\s+/g, ' ')
        .trim();

      return label || 'Action';
    }

    function isExecutableScriptLine(line) {
      return line.length > 0 &&
        !line.startsWith('#') &&
        !line.startsWith('//');
    }

    function getScriptCommandsForAction(button) {
      const payload = readPayload(button.dataset.payload);

      switch (button.dataset.action) {
        case 'say':
          const message = String(payload.message || '').trim();
          if (!isValidSayMessage(message)) {
            setStatus('Say message must be 1-200 characters and cannot contain semicolons, quotes, or newlines', 'error');
            return [];
          }
          return [`say ${message}`];
        case 'competitive_offline':
          return ['exec gamemode_competitive_offline'];
        case 'restart_game':
          return [`mp_restartgame ${clampNumber(payload.delaySeconds, 1, 0, 60)}`];
        case 'warmup_start':
          return ['mp_warmup_start'];
        case 'warmup_end':
          return ['mp_warmup_end'];
        case 'pause_match':
          return ['mp_pause_match'];
        case 'unpause_match':
          return ['mp_unpause_match'];
        case 'overtime_on':
          return [
            'mp_overtime_enable 1',
            `mp_overtime_maxrounds ${clampNumber(payload.maxRounds, 6, 2, 30)}`,
            `mp_overtime_startmoney ${clampNumber(payload.startMoney, 10000, 800, 65535)}`,
            `mp_overtime_limit ${clampNumber(payload.limit, 0, 0, 30)}`
          ];
        case 'overtime_off':
          return ['mp_overtime_enable 0'];
        case 'friendly_fire_on':
          return ['mp_friendlyfire 1'];
        case 'friendly_fire_off':
          return ['mp_friendlyfire 0'];
        case 'set_freezetime':
          return [`mp_freezetime ${clampNumber(payload.seconds, 15, 0, 60)}`];
        case 'set_buytime':
          return [`mp_buytime ${clampNumber(payload.seconds, 20, 0, 600)}`];
        case 'set_startmoney':
          return [`mp_startmoney ${clampNumber(payload.money, 800, 800, 65535)}`];
        case 'set_roundtime': {
          const minutes = clampNumber(payload.minutes, 2, 1, 60);
          return [
            `mp_roundtime ${minutes}`,
            `mp_roundtime_defuse ${minutes}`,
            `mp_roundtime_hostage ${minutes}`
          ];
        }
        case 'set_maxrounds':
          return [`mp_maxrounds ${clampNumber(payload.rounds, 24, 1, 60)}`];
        case 'bot_quota':
          return [`bot_quota ${clampNumber(payload.quota, 10, 0, 64)}`];
        case 'bot_difficulty':
          return [`bot_difficulty ${clampNumber(payload.difficulty, 2, 0, 3)}`];
        case 'bot_kick':
          return ['bot_kick'];
        case 'change_map':
          return [`changelevel ${button.dataset.mapName}`];
        default:
          setStatus(`Action "${button.dataset.action}" cannot be added to a script yet`, 'error');
          return [];
      }
    }

    function clampNumber(value, fallback, min, max) {
      const parsed = Number(value);
      const number = Number.isFinite(parsed) ? parsed : fallback;
      return Math.min(max, Math.max(min, number));
    }

    function isValidSayMessage(message) {
      return message.length > 0 &&
        message.length <= 200 &&
        !message.includes(';') &&
        !message.includes('"') &&
        !message.includes('\r') &&
        !message.includes('\n');
    }

    async function saveScript(button) {
      await withBusy(button, async () => {
        const response = await fetch('/api/ui/scripts/save', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            scriptFileName: scriptFileName.value,
            content: scriptContent.value
          })
        });
        const payload = await readResponsePayload(response);

        if (payload.ok) {
          scriptFileName.value = payload.result.name;
          scriptContent.value = payload.result.content;
          scriptMeta.textContent = `${payload.result.fileName} - ${payload.result.commandCount} commands`;
          await loadScripts();
        }

        showPayload(payload);
      });
    }

    async function withBusy(button, work) {
      const buttons = [...document.querySelectorAll('button')];
      buttons.forEach(item => item.disabled = true);
      setStatus('Running...', '');
      try {
        await work();
      } catch (error) {
        showPayload({ ok: false, error: error.message });
      } finally {
        buttons.forEach(item => item.disabled = false);
      }
    }

    function readPayload(spec) {
      if (!spec) {
        return {};
      }

      const payload = {};
      for (const part of spec.split(',')) {
        const [name, selector] = part.split(':');
        const input = document.querySelector(selector);
        if (!input) {
          continue;
        }

        payload[name] = input.type === 'number' ? Number(input.value) : input.value;
      }
      return payload;
    }

    async function readResponsePayload(response) {
      const contentType = (response.headers.get('content-type') || '').toLowerCase();
      const text = await response.text();
      let payload = null;

      if (text && contentType.includes('json')) {
        try {
          payload = JSON.parse(text);
        } catch (error) {
          payload = null;
        }
      }

      if (payload && typeof payload === 'object') {
        if (response.ok) {
          return payload;
        }

        return {
          ...payload,
          ok: false,
          error: getErrorMessage(payload)
        };
      }

      if (response.ok) {
        return { ok: true, result: text };
      }

      const suffix = text ? `: ${text}` : '';
      return { ok: false, error: `HTTP ${response.status} ${response.statusText}${suffix}` };
    }

    function showPayload(payload) {
      if (payload.ok) {
        setStatus('OK', 'ok');
        output.textContent = typeof payload.result === 'string'
          ? payload.result || '(empty response)'
          : JSON.stringify(payload.result, null, 2);
      } else {
        const error = getErrorMessage(payload);
        setStatus(`Error: ${error}`, 'error');
        output.textContent = error;
      }
    }

    function getErrorMessage(payload) {
      if (!payload) {
        return 'Unknown error';
      }

      return payload.error ||
        payload.detail ||
        payload.title ||
        payload.message ||
        'Unknown error';
    }

    function setStatus(text, kind) {
      statusLine.textContent = text;
      statusLine.className = `statusline ${kind}`;
    }
  </script>
</body>
</html>
""";
    }
}

internal sealed record WebUiActionRequest(
    string? Message = null,
    string? MapName = null,
    int? DelaySeconds = null,
    int? MaxRounds = null,
    int? StartMoney = null,
    int? Limit = null,
    int? Seconds = null,
    int? Money = null,
    int? Minutes = null,
    int? Rounds = null,
    int? Quota = null,
    int? Difficulty = null);

internal sealed record WebUiScriptRunRequest(
    string? ScriptFileName = null);

internal sealed record WebUiScriptSaveRequest(
    string? ScriptFileName = null,
    string? Content = null);
