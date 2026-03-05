using GlobalActivityBot.Domain.Entities;
using GlobalActivityBot.Infrastructure.Data;
using GlobalActivityBot.Services;
using GlobalActivityBot.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GlobalActivityBot.Tests;

public class StatsServiceTests
{
    private static BotDbContext CreateInMemoryDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<BotDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new BotDbContext(options);
    }

    [Fact]
    public async Task AddXpAsync_CreatesUserStat_WhenNoneExists()
    {
        var db = CreateInMemoryDb(nameof(AddXpAsync_CreatesUserStat_WhenNoneExists));

        var userServiceMock = new Mock<IUserService>();
        userServiceMock
            .Setup(s => s.GetOrCreateUserAsync(It.IsAny<ulong>(), It.IsAny<string>()))
            .ReturnsAsync(new User { Id = 1, DiscordId = "123", Username = "TestUser" });

        var guildServiceMock = new Mock<IGuildService>();
        guildServiceMock
            .Setup(s => s.GetOrCreateGuildAsync(It.IsAny<ulong>(), It.IsAny<string>()))
            .ReturnsAsync(new Guild { Id = 1, DiscordId = "456", Name = "TestGuild" });

        var statsService = new StatsService(db, userServiceMock.Object, guildServiceMock.Object, NullLogger<StatsService>.Instance);

        await statsService.AddXpAsync(123UL, 456UL, "TestGuild");

        var stat = await db.UserStats.FirstOrDefaultAsync();
        Assert.NotNull(stat);
        Assert.Equal(15, stat.Xp);
        Assert.Equal(1, stat.MessageCount);
    }

    [Fact]
    public async Task AddXpAsync_AccumulatesXp_WhenStatExists()
    {
        var db = CreateInMemoryDb(nameof(AddXpAsync_AccumulatesXp_WhenStatExists));

        var existingStat = new UserStat { Id = 1, UserId = 1, GuildId = 1, Xp = 100, Level = 3, MessageCount = 10 };
        db.UserStats.Add(existingStat);
        await db.SaveChangesAsync();

        var userServiceMock = new Mock<IUserService>();
        userServiceMock
            .Setup(s => s.GetOrCreateUserAsync(It.IsAny<ulong>(), It.IsAny<string>()))
            .ReturnsAsync(new User { Id = 1, DiscordId = "123", Username = "TestUser" });

        var guildServiceMock = new Mock<IGuildService>();
        guildServiceMock
            .Setup(s => s.GetOrCreateGuildAsync(It.IsAny<ulong>(), It.IsAny<string>()))
            .ReturnsAsync(new Guild { Id = 1, DiscordId = "456", Name = "TestGuild" });

        var statsService = new StatsService(db, userServiceMock.Object, guildServiceMock.Object, NullLogger<StatsService>.Instance);

        await statsService.AddXpAsync(123UL, 456UL, "TestGuild");

        var stat = await db.UserStats.FirstAsync();
        Assert.Equal(115, stat.Xp);
        Assert.Equal(11, stat.MessageCount);
    }

    [Fact]
    public async Task AddXpAsync_CalculatesLevel_Correctly()
    {
        var db = CreateInMemoryDb(nameof(AddXpAsync_CalculatesLevel_Correctly));

        var userServiceMock = new Mock<IUserService>();
        userServiceMock
            .Setup(s => s.GetOrCreateUserAsync(It.IsAny<ulong>(), It.IsAny<string>()))
            .ReturnsAsync(new User { Id = 1, DiscordId = "123", Username = "TestUser" });

        var guildServiceMock = new Mock<IGuildService>();
        guildServiceMock
            .Setup(s => s.GetOrCreateGuildAsync(It.IsAny<ulong>(), It.IsAny<string>()))
            .ReturnsAsync(new Guild { Id = 1, DiscordId = "456", Name = "TestGuild" });

        var statsService = new StatsService(db, userServiceMock.Object, guildServiceMock.Object, NullLogger<StatsService>.Instance);

        // 1000 XP should give level 10 (floor(sqrt(1000/10)) = floor(sqrt(100)) = 10)
        await statsService.AddXpAsync(123UL, 456UL, "TestGuild", xpAmount: 1000);

        var stat = await db.UserStats.FirstAsync();
        Assert.Equal(10, stat.Level);
    }

    [Fact]
    public async Task SyncGlobalXpAsync_UpdatesGlobalXp_FromUserStats()
    {
        var db = CreateInMemoryDb(nameof(SyncGlobalXpAsync_UpdatesGlobalXp_FromUserStats));

        var user = new User { Id = 1, DiscordId = "123", Username = "TestUser", GlobalXp = 0 };
        db.Users.Add(user);
        db.UserStats.AddRange(
            new UserStat { UserId = 1, GuildId = 1, Xp = 200 },
            new UserStat { UserId = 1, GuildId = 2, Xp = 300 }
        );
        await db.SaveChangesAsync();

        var userServiceMock = new Mock<IUserService>();
        var guildServiceMock = new Mock<IGuildService>();
        var statsService = new StatsService(db, userServiceMock.Object, guildServiceMock.Object, NullLogger<StatsService>.Instance);

        await statsService.SyncGlobalXpAsync();

        var updatedUser = await db.Users.FindAsync(1);
        Assert.NotNull(updatedUser);
        Assert.Equal(500, updatedUser.GlobalXp);
        // floor(sqrt(500/10)) = floor(sqrt(50)) = floor(7.07) = 7
        Assert.Equal(7, updatedUser.GlobalLevel);
    }
}

public class LevelFormulaTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(10, 1)]
    [InlineData(40, 2)]
    [InlineData(90, 3)]
    [InlineData(1000, 10)]
    public void LevelFormula_ReturnsExpectedLevel(int xp, int expectedLevel)
    {
        var level = (int)Math.Floor(Math.Sqrt(xp / 10.0));
        Assert.Equal(expectedLevel, level);
    }
}

public class UserServiceTests
{
    private static BotDbContext CreateInMemoryDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<BotDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new BotDbContext(options);
    }

    [Fact]
    public async Task GetOrCreateUserAsync_CreatesUser_WhenNotExists()
    {
        var db = CreateInMemoryDb(nameof(GetOrCreateUserAsync_CreatesUser_WhenNotExists));
        var service = new UserService(db, NullLogger<UserService>.Instance);

        var user = await service.GetOrCreateUserAsync(12345UL, "TestUser");

        Assert.NotNull(user);
        Assert.Equal("12345", user.DiscordId);
        Assert.Equal("TestUser", user.Username);
    }

    [Fact]
    public async Task GetOrCreateUserAsync_ReturnsExistingUser_WhenExists()
    {
        var db = CreateInMemoryDb(nameof(GetOrCreateUserAsync_ReturnsExistingUser_WhenExists));
        db.Users.Add(new User { Id = 1, DiscordId = "12345", Username = "ExistingUser" });
        await db.SaveChangesAsync();

        var service = new UserService(db, NullLogger<UserService>.Instance);

        var user = await service.GetOrCreateUserAsync(12345UL, "NewName");

        Assert.Equal("NewName", user.Username); // Username updated to reflect current name
        Assert.Equal(1, await db.Users.CountAsync());
    }
}

public class GuildServiceTests
{
    private static BotDbContext CreateInMemoryDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<BotDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new BotDbContext(options);
    }

    [Fact]
    public async Task GetOrCreateGuildAsync_CreatesGuild_WhenNotExists()
    {
        var db = CreateInMemoryDb(nameof(GetOrCreateGuildAsync_CreatesGuild_WhenNotExists));
        var service = new GuildService(db, NullLogger<GuildService>.Instance);

        var guild = await service.GetOrCreateGuildAsync(99999UL, "TestGuild");

        Assert.NotNull(guild);
        Assert.Equal("99999", guild.DiscordId);
        Assert.Equal("TestGuild", guild.Name);
    }
}
