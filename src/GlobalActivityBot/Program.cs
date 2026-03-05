using DSharpPlus;
using DSharpPlus.Commands;
using Microsoft.Extensions.Configuration;
using GlobalActivityBot.Bot;
using GlobalActivityBot.Bot.EventHandlers;
using GlobalActivityBot.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting GlobalActivityBot");

    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .AddEnvironmentVariables()
        .Build();

    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .WriteTo.Console(new CompactJsonFormatter())
        .CreateLogger();

    var token = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN")
        ?? configuration["Bot:Token"]
        ?? throw new InvalidOperationException("DISCORD_BOT_TOKEN is not set.");

    var discordBuilder = DiscordClientBuilder
        .CreateDefault(token, DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents)
        .ConfigureLogging(logging => logging.AddSerilog())
        .ConfigureServices(services =>
        {
            services.Configure<BotOptions>(configuration.GetSection("Bot"));
            services.Configure<DatabaseOptions>(configuration.GetSection("Database"));
            services.AddBotServices(configuration);
        })
        .UseCommands((sp, ext) =>
        {
            ext.AddCommands(typeof(Program).Assembly);
        })
        .ConfigureEventHandlers(b =>
        {
            b.AddEventHandlers<XpMessageHandler>(ServiceLifetime.Scoped);
            b.AddEventHandlers<GuildJoinHandler>(ServiceLifetime.Scoped);
        });

    var client = discordBuilder.Build();

    await client.ConnectAsync();

    Log.Information("Bot connected successfully");

    await Task.Delay(-1);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Bot terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
