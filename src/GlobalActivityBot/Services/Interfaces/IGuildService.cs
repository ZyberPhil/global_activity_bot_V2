using GlobalActivityBot.Domain.Entities;

namespace GlobalActivityBot.Services.Interfaces;

public interface IGuildService
{
    Task<Guild> GetOrCreateGuildAsync(ulong discordId, string name);
    Task<Guild?> GetGuildByDiscordIdAsync(ulong discordId);
}
