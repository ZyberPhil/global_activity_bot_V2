using GlobalActivityBot.Domain.Entities;
using GlobalActivityBot.Infrastructure.Data;
using GlobalActivityBot.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GlobalActivityBot.Services;

public class ProfileService : IProfileService
{
    private readonly BotDbContext _db;
    private readonly IUserService _userService;
    private readonly IGuildService _guildService;
    private readonly IBadgeService _badgeService;

    public ProfileService(BotDbContext db, IUserService userService, IGuildService guildService, IBadgeService badgeService)
    {
        _db = db;
        _userService = userService;
        _guildService = guildService;
        _badgeService = badgeService;
    }

    public async Task<ProfileData?> GetProfileAsync(ulong userDiscordId, ulong guildDiscordId, string guildName)
    {
        var user = await _userService.GetUserByDiscordIdAsync(userDiscordId);
        if (user == null) return null;

        var guild = await _guildService.GetOrCreateGuildAsync(guildDiscordId, guildName);

        var guildStat = await _db.UserStats
            .FirstOrDefaultAsync(s => s.UserId == user.Id && s.GuildId == guild.Id);

        var badges = await _badgeService.GetUserBadgesAsync(userDiscordId);

        return new ProfileData(user, guildStat, badges);
    }
}
