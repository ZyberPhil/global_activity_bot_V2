using GlobalActivityBot.Domain.Entities;

namespace GlobalActivityBot.Services.Interfaces;

public interface IBadgeService
{
    Task<Badge?> GetBadgeByNameAsync(string name);
    Task<IReadOnlyList<Badge>> GetAllBadgesAsync();
    Task<IReadOnlyList<Badge>> GetUserBadgesAsync(ulong userDiscordId);
    Task<bool> GiveBadgeToUserAsync(ulong userDiscordId, string badgeName);
}
