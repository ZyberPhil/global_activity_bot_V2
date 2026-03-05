using DSharpPlus;
using DSharpPlus.EventArgs;
using GlobalActivityBot.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace GlobalActivityBot.Bot.EventHandlers;

public class GuildJoinHandler : IEventHandler<GuildCreatedEventArgs>
{
    private readonly IGuildService _guildService;
    private readonly ILogger<GuildJoinHandler> _logger;

    public GuildJoinHandler(IGuildService guildService, ILogger<GuildJoinHandler> logger)
    {
        _guildService = guildService;
        _logger = logger;
    }

    public async Task HandleEventAsync(DiscordClient sender, GuildCreatedEventArgs eventArgs)
    {
        try
        {
            await _guildService.GetOrCreateGuildAsync(eventArgs.Guild.Id, eventArgs.Guild.Name);
            _logger.LogInformation("Bot joined guild {GuildName} ({GuildId})", eventArgs.Guild.Name, eventArgs.Guild.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register guild {GuildId}", eventArgs.Guild.Id);
        }
    }
}
