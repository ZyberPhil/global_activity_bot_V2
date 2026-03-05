using GlobalActivityBot.Domain.Entities;

namespace GlobalActivityBot.Services.Interfaces;

public interface IUserService
{
    Task<User> GetOrCreateUserAsync(ulong discordId, string username);
    Task<User?> GetUserByDiscordIdAsync(ulong discordId);
}
