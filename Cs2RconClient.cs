using System.Net;
using CoreRCON;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol;

internal sealed class Cs2RconClient
{
    private const int DefaultMaxResponseCharacters = 12000;
    private const int MaximumResponseCharacters = 50000;

    private readonly IConfiguration _configuration;

    public Cs2RconClient(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string> SendCommandAsync(string command, int maxCharacters = DefaultMaxResponseCharacters)
    {
        var rcon = await CreateConnectedClientAsync();
        var response = await rcon.SendCommandAsync(command);

        return LimitResponse(response, maxCharacters);
    }

    public async Task<IReadOnlyDictionary<string, string>> SendCommandsAsync(IEnumerable<string> commands, int maxCharactersPerCommand = DefaultMaxResponseCharacters)
    {
        var rcon = await CreateConnectedClientAsync();
        var responses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var command in commands)
        {
            var response = await rcon.SendCommandAsync(command);
            responses[command] = LimitResponse(response, maxCharactersPerCommand);
        }

        return responses;
    }

    public async Task<IReadOnlyList<Cs2CommandResult>> SendCommandSequenceAsync(IEnumerable<string> commands, int maxCharactersPerCommand = DefaultMaxResponseCharacters)
    {
        var rcon = await CreateConnectedClientAsync();
        var results = new List<Cs2CommandResult>();

        foreach (var command in commands)
        {
            var response = await rcon.SendCommandAsync(command);
            results.Add(new Cs2CommandResult(command, LimitResponse(response, maxCharactersPerCommand)));
        }

        return results;
    }

    private async Task<RCON> CreateConnectedClientAsync()
    {
        var host = GetSetting("CS2:Rcon:Host", "CS2_RCON_HOST") ?? "127.0.0.1";
        var port = GetIntSetting("CS2:Rcon:Port", "CS2_RCON_PORT") ?? Program.DefaultRconPort;
        var password = GetSetting("CS2:Rcon:Password", "CS2_RCON_PASSWORD");

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new McpException("CS2 RCON password is not configured. Set CS2_RCON_PASSWORD or CS2:Rcon:Password.");
        }

        var address = await ResolveAddressAsync(host);
        var rcon = new RCON(new IPEndPoint(address, port), password);

        await rcon.ConnectAsync();
        return rcon;
    }

    private string? GetSetting(string configurationKey, string environmentVariable)
    {
        return _configuration[configurationKey] ?? Environment.GetEnvironmentVariable(environmentVariable);
    }

    private int? GetIntSetting(string configurationKey, string environmentVariable)
    {
        var value = GetSetting(configurationKey, environmentVariable);
        return int.TryParse(value, out var parsed) ? parsed : null;
    }

    private static async Task<IPAddress> ResolveAddressAsync(string host)
    {
        if (IPAddress.TryParse(host, out var address))
        {
            return address;
        }

        var addresses = await Dns.GetHostAddressesAsync(host);
        return addresses.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) ??
            addresses.FirstOrDefault() ??
            throw new McpException($"Could not resolve CS2 RCON host '{host}'.");
    }

    private static string LimitResponse(string response, int maxCharacters)
    {
        maxCharacters = Math.Clamp(maxCharacters, 1000, MaximumResponseCharacters);

        if (response.Length <= maxCharacters)
        {
            return response;
        }

        return response[..maxCharacters] + $"\n... truncated to {maxCharacters} characters";
    }
}
