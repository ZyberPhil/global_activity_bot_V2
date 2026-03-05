using GlobalActivityBot.Domain.Entities;
using GlobalActivityBot.Infrastructure.Data;
using GlobalActivityBot.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GlobalActivityBot.Services;

public class BadgeService : IBadgeService
{
    private readonly BotDbContext _db;
    private readonly IUserService _userService;
    private readonly ILogger<BadgeService> _logger;

    public BadgeService(BotDbContext db, IUserService userService, ILogger<BadgeService> logger)
    {
        _db = db;
        _userService = userService;
        _logger = logger;
    }

    public async Task<Badge?> GetBadgeByNameAsync(string name)
        => await _db.Badges.FirstOrDefaultAsync(b => b.Name == name);

    public async Task<IReadOnlyList<Badge>> GetAllBadgesAsync()
        => await _db.Badges.OrderBy(b => b.Name).ToListAsync();

    public async Task<IReadOnlyList<Badge>> GetUserBadgesAsync(ulong userDiscordId)
    {
        var idStr = userDiscordId.ToString();
        return await _db.UserBadges
            .Include(ub => ub.Badge)
            .Include(ub => ub.User)
            .Where(ub => ub.User.DiscordId == idStr)
            .Select(ub => ub.Badge)
            .ToListAsync();
    }

    public async Task<bool> GiveBadgeToUserAsync(ulong userDiscordId, string badgeName)
    {
        var user = await _userService.GetUserByDiscordIdAsync(userDiscordId);
        if (user == null) return false;

        var badge = await GetBadgeByNameAsync(badgeName);
        if (badge == null) return false;

        var alreadyHas = await _db.UserBadges.AnyAsync(ub => ub.UserId == user.Id && ub.BadgeId == badge.Id);
        if (alreadyHas) return false;

        _db.UserBadges.Add(new UserBadge
        {
            UserId = user.Id,
            BadgeId = badge.Id,
            AwardedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        _logger.LogInformation("Awarded badge {Badge} to user {Username}", badgeName, user.Username);
        return true;
    }
}
