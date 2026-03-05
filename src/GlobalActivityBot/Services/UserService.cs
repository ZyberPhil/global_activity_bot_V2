using GlobalActivityBot.Domain.Entities;
using GlobalActivityBot.Infrastructure.Data;
using GlobalActivityBot.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GlobalActivityBot.Services;

public class UserService : IUserService
{
    private readonly BotDbContext _db;
    private readonly ILogger<UserService> _logger;

    public UserService(BotDbContext db, ILogger<UserService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<User> GetOrCreateUserAsync(ulong discordId, string username)
    {
        var idStr = discordId.ToString();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.DiscordId == idStr);
        if (user != null)
        {
            if (user.Username != username)
            {
                user.Username = username;
                await _db.SaveChangesAsync();
            }
            return user;
        }

        user = new User
        {
            DiscordId = idStr,
            Username = username,
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Created new user {Username} ({DiscordId})", username, discordId);
        return user;
    }

    public async Task<User?> GetUserByDiscordIdAsync(ulong discordId)
    {
        var idStr = discordId.ToString();
        return await _db.Users.FirstOrDefaultAsync(u => u.DiscordId == idStr);
    }
}
