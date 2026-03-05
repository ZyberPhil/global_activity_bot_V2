using DSharpPlus;
using DSharpPlus.EventArgs;
using GlobalActivityBot.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GlobalActivityBot.Bot.EventHandlers;

public class XpMessageHandler : IEventHandler<MessageCreatedEventArgs>
{
    private static readonly TimeSpan CooldownDuration = TimeSpan.FromSeconds(60);

    private readonly IStatsService _statsService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<XpMessageHandler> _logger;

    public XpMessageHandler(IStatsService statsService, IMemoryCache cache, ILogger<XpMessageHandler> logger)
    {
        _statsService = statsService;
        _cache = cache;
        _logger = logger;
    }

    public async Task HandleEventAsync(DiscordClient sender, MessageCreatedEventArgs eventArgs)
    {
        if (eventArgs.Author?.IsBot == true) return;
        if (eventArgs.Guild is null) return;

        var userId = eventArgs.Author!.Id;
        var guildId = eventArgs.Guild.Id;
        var cacheKey = $"xp_cooldown:{userId}:{guildId}";

        if (_cache.TryGetValue(cacheKey, out _))
            return;

        _cache.Set(cacheKey, true, CooldownDuration);

        try
        {
            await _statsService.AddXpAsync(userId, guildId, eventArgs.Guild.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add XP for user {UserId} in guild {GuildId}", userId, guildId);
        }
    }
}
