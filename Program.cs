using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

internal static class Program
{
    internal const int DefaultRconPort = 27015;

    private const string DefaultServerUrl = "http://127.0.0.1:3001";
    private const string McpHttpPath = "/mcp";

    public static async Task Main(string[] args)
    {
        RegisterProcessDiagnostics();
        DiagnosticsLog.Write($"Process started. CommandLine='{Environment.CommandLine}'. BaseDirectory='{AppContext.BaseDirectory}'. CurrentDirectory='{Environment.CurrentDirectory}'.");

        try
        {
            var commandLine = ParseCommandLine(args);

            if (UseHttpTransport(commandLine))
            {
                await RunHttpServerAsync(args);
            }
            else
            {
                await RunStdioServerAsync(args);
            }
        }
        catch (Exception ex)
        {
            DiagnosticsLog.Write("Fatal application error.", ex);
            throw;
        }
        finally
        {
            DiagnosticsLog.Write("Main method exiting.");
        }
    }

    private static void RegisterProcessDiagnostics()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) => DiagnosticsLog.Write("ProcessExit event.");
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var exception = e.ExceptionObject as Exception;
            DiagnosticsLog.Write($"Unhandled exception. IsTerminating={e.IsTerminating}.", exception);
        };
        Console.CancelKeyPress += (_, e) =>
        {
            DiagnosticsLog.Write($"Console CancelKeyPress event. SpecialKey={e.SpecialKey}.");
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            DiagnosticsLog.Write("Unobserved task exception.", e.Exception);
        };
    }

    private static async Task RunStdioServerAsync(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        var commandLine = ParseCommandLine(args);
        var webUiUrl = GetServerUrl(commandLine);

        // Stdio MCP must keep stdout reserved for protocol messages.
        builder.Logging.ClearProviders();

        AddCommonMcpServer(builder.Services)
            .WithStdioServerTransport();

        using var mcpHost = builder.Build();
        RegisterHostLifetimeDiagnostics("stdio MCP host", mcpHost.Services);

        EnsureScriptsDirectoryExists(mcpHost.Services);

        using var webUiCancellation = new CancellationTokenSource();
        var webUiTask = RunResilientWebUiAsync(args, commandLine, webUiUrl, webUiCancellation.Token);

        DiagnosticsLog.Write($"Starting stdio MCP server with Web UI target at {webUiUrl}/ui.");
        try
        {
            await RunHostWithDiagnosticsAsync("stdio MCP host", mcpHost, CancellationToken.None);
        }
        finally
        {
            webUiCancellation.Cancel();

            try
            {
                await webUiTask;
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    private static async Task RunHttpServerAsync(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        if (string.IsNullOrWhiteSpace(builder.Configuration["urls"]) &&
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
        {
            builder.WebHost.UseUrls(DefaultServerUrl);
        }

        AddCommonMcpServer(builder.Services)
            .WithHttpTransport(options =>
            {
                options.Stateless = true;
            });

        var app = builder.Build();
        RegisterHostLifetimeDiagnostics("HTTP MCP/Web UI host", app.Services);

        EnsureScriptsDirectoryExists(app.Services);

        WebUi.Map(app, McpHttpPath);
        app.MapMcp(McpHttpPath);

        DiagnosticsLog.Write("Starting HTTP MCP server with Web UI.");
        await app.RunAsync();
    }

    private static WebApplication BuildWebUiApp(string[] args, IConfiguration configuration)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls(GetServerUrl(configuration));
        AddCommonServices(builder.Services);

        var app = builder.Build();
        RegisterHostLifetimeDiagnostics("Web UI host", app.Services);
        WebUi.Map(app, null);

        return app;
    }

    private static void RegisterHostLifetimeDiagnostics(string name, IServiceProvider services)
    {
        var lifetime = services.GetService<IHostApplicationLifetime>();
        if (lifetime is null)
        {
            DiagnosticsLog.Write($"{name}: IHostApplicationLifetime is not available.");
            return;
        }

        lifetime.ApplicationStarted.Register(() => DiagnosticsLog.Write($"{name}: ApplicationStarted."));
        lifetime.ApplicationStopping.Register(() => DiagnosticsLog.Write($"{name}: ApplicationStopping."));
        lifetime.ApplicationStopped.Register(() => DiagnosticsLog.Write($"{name}: ApplicationStopped."));
    }

    private static async Task RunResilientWebUiAsync(
        string[] args,
        IConfiguration configuration,
        string webUiUrl,
        CancellationToken cancellationToken)
    {
        var failureCount = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var webApp = BuildWebUiApp(args, configuration);

                DiagnosticsLog.Write($"Starting Web UI host at {webUiUrl}/ui.");
                await webApp.RunAsync(cancellationToken);
                DiagnosticsLog.Write("Web UI host stopped.");
                failureCount = 0;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                DiagnosticsLog.Write("Web UI host canceled.");
                return;
            }
            catch (Exception ex)
            {
                failureCount++;
                if (failureCount <= 3 || failureCount % 30 == 0)
                {
                    DiagnosticsLog.Write($"Web UI host failed; retrying in 2 seconds. Failure #{failureCount}.", ex);
                }
                else
                {
                    DiagnosticsLog.Write($"Web UI host failed; retrying in 2 seconds. Failure #{failureCount}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
    }

    private static async Task RunHostWithDiagnosticsAsync(string name, IHost host, CancellationToken cancellationToken)
    {
        try
        {
            await host.RunAsync(cancellationToken);
            DiagnosticsLog.Write($"{name} stopped.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            DiagnosticsLog.Write($"{name} canceled.");
        }
        catch (Exception ex)
        {
            DiagnosticsLog.Write($"{name} failed.", ex);
            throw;
        }
    }

    private static IMcpServerBuilder AddCommonMcpServer(IServiceCollection services)
    {
        AddCommonServices(services);

        return services
            .AddMcpServer(options =>
            {
                options.ServerInfo = new Implementation
                {
                    Name = "cs2-mcp-server",
                    Version = "1.0.0"
                };
                options.ServerInstructions = "Use the CS2 RCON tools to inspect and administer a Counter-Strike 2 server.";
            })
            .WithToolsFromAssembly();
    }

    private static void AddCommonServices(IServiceCollection services)
    {
        services.AddSingleton<Cs2RconClient>();
        services.AddSingleton<Cs2ScriptService>();
    }

    private static IConfigurationRoot ParseCommandLine(string[] args)
    {
        return new ConfigurationBuilder()
            .AddCommandLine(args)
            .Build();
    }

    private static bool UseHttpTransport(IConfiguration configuration)
    {
        return string.Equals(configuration["transport"], "http", StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureScriptsDirectoryExists(IServiceProvider services)
    {
        services.GetRequiredService<Cs2ScriptService>().EnsureScriptsDirectoryExists();
    }

    private static string GetServerUrl(IConfiguration configuration)
    {
        return (configuration["urls"] ??
            Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ??
            configuration["web-ui-url"] ??
            configuration["webUiUrl"] ??
            configuration["CS2:WebUi:Url"] ??
            Environment.GetEnvironmentVariable("CS2_WEB_UI_URL") ??
            DefaultServerUrl).TrimEnd('/');
    }
}
