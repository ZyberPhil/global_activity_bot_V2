using GlobalActivityBot.Domain.Entities;
using GlobalActivityBot.Infrastructure.Data;
using GlobalActivityBot.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GlobalActivityBot.Services;

public class StatsService : IStatsService
{
    private readonly BotDbContext _db;
    private readonly IUserService _userService;
    private readonly IGuildService _guildService;
    private readonly ILogger<StatsService> _logger;

    public StatsService(BotDbContext db, IUserService userService, IGuildService guildService, ILogger<StatsService> logger)
    {
        _db = db;
        _userService = userService;
        _guildService = guildService;
        _logger = logger;
    }

    public async Task AddXpAsync(ulong userDiscordId, ulong guildDiscordId, string guildName, int xpAmount = 15)
    {
        var user = await _userService.GetOrCreateUserAsync(userDiscordId, userDiscordId.ToString());
        var guild = await _guildService.GetOrCreateGuildAsync(guildDiscordId, guildName);

        var stat = await _db.UserStats.FirstOrDefaultAsync(s => s.UserId == user.Id && s.GuildId == guild.Id);
        if (stat == null)
        {
            stat = new UserStat
            {
                UserId = user.Id,
                GuildId = guild.Id,
                Xp = 0,
                Level = 0,
                MessageCount = 0,
                LastMessageAt = DateTime.UtcNow
            };
            _db.UserStats.Add(stat);
        }

        stat.Xp += xpAmount;
        stat.MessageCount++;
        stat.LastMessageAt = DateTime.UtcNow;
        stat.Level = (int)Math.Floor(Math.Sqrt(stat.Xp / 10.0));

        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<(User User, UserStat Stat)>> GetGuildLeaderboardAsync(ulong guildDiscordId, int page = 1, int pageSize = 10)
    {
        var guildIdStr = guildDiscordId.ToString();
        var guild = await _db.Guilds.FirstOrDefaultAsync(g => g.DiscordId == guildIdStr);
        if (guild == null) return Array.Empty<(User, UserStat)>();

        var results = await _db.UserStats
            .Where(s => s.GuildId == guild.Id)
            .OrderByDescending(s => s.Xp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(s => s.User)
            .Select(s => new { s.User, Stat = s })
            .ToListAsync();

        return results.Select(r => (r.User, r.Stat)).ToList();
    }

    public async Task<IReadOnlyList<(User User, int GlobalXp)>> GetGlobalLeaderboardAsync(int page = 1, int pageSize = 10)
    {
        var results = await _db.Users
            .OrderByDescending(u => u.GlobalXp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return results.Select(u => (u, u.GlobalXp)).ToList();
    }

    public async Task SyncGlobalXpAsync()
    {
        var xpTotals = await _db.UserStats
            .GroupBy(s => s.UserId)
            .Select(g => new { UserId = g.Key, TotalXp = g.Sum(s => s.Xp) })
            .ToDictionaryAsync(x => x.UserId, x => x.TotalXp);

        var users = await _db.Users.ToListAsync();
        foreach (var user in users)
        {
            var totalXp = xpTotals.GetValueOrDefault(user.Id, 0);
            user.GlobalXp = totalXp;
            user.GlobalLevel = (int)Math.Floor(Math.Sqrt(totalXp / 10.0));
        }
        await _db.SaveChangesAsync();
        _logger.LogInformation("Synced global XP for {Count} users", users.Count);
    }
}
