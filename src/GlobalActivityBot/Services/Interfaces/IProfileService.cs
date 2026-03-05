using GlobalActivityBot.Domain.Entities;

namespace GlobalActivityBot.Services.Interfaces;

public record ProfileData(User User, UserStat? GuildStat, IReadOnlyList<Badge> Badges);

public interface IProfileService
{
    Task<ProfileData?> GetProfileAsync(ulong userDiscordId, ulong guildDiscordId, string guildName);
}
