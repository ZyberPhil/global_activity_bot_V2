using GlobalActivityBot.Domain.Entities;
using GlobalActivityBot.Infrastructure.Data;
using GlobalActivityBot.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GlobalActivityBot.Services;

public class GuildService : IGuildService
{
    private readonly BotDbContext _db;
    private readonly ILogger<GuildService> _logger;

    public GuildService(BotDbContext db, ILogger<GuildService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Guild> GetOrCreateGuildAsync(ulong discordId, string name)
    {
        var idStr = discordId.ToString();
        var guild = await _db.Guilds.FirstOrDefaultAsync(g => g.DiscordId == idStr);
        if (guild != null)
        {
            if (guild.Name != name)
            {
                guild.Name = name;
                await _db.SaveChangesAsync();
            }
            return guild;
        }

        guild = new Guild
        {
            DiscordId = idStr,
            Name = name,
            CreatedAt = DateTime.UtcNow
        };
        _db.Guilds.Add(guild);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Registered guild {Name} ({DiscordId})", name, discordId);
        return guild;
    }

    public async Task<Guild?> GetGuildByDiscordIdAsync(ulong discordId)
    {
        var idStr = discordId.ToString();
        return await _db.Guilds.FirstOrDefaultAsync(g => g.DiscordId == idStr);
    }
}
