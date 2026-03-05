using GlobalActivityBot.Domain.Entities;

namespace GlobalActivityBot.Services.Interfaces;

public interface IStatsService
{
    Task AddXpAsync(ulong userDiscordId, ulong guildDiscordId, string guildName, int xpAmount = 15);
    Task<IReadOnlyList<(User User, UserStat Stat)>> GetGuildLeaderboardAsync(ulong guildDiscordId, int page = 1, int pageSize = 10);
    Task<IReadOnlyList<(User User, int GlobalXp)>> GetGlobalLeaderboardAsync(int page = 1, int pageSize = 10);
    Task SyncGlobalXpAsync();
}
